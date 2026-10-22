# Database strategy, initialization and safety

For each database choose **one** authoritative schema-management strategy. Initialization must finish before any
resource that needs the schema starts (`WaitForCompletion`), run once per startup, not once per application instance,
and be safe to repeat against an existing database.

## Choosing the strategy

| Evidence | Strategy |
|---|---|
| `DbContext` + a consistent `Migrations/` folder with a model snapshot | **A** EF Core migrations |
| EF Core used, but no migrations (or entities clearly reverse-engineered), schema owned by SQL/DBA | **B** EF Core database-first |
| Dapper / ADO.NET / stored procedures, with SQL scripts or a migration tool (DbUp, FluentMigrator, Evolve, Flyway, Liquibase, `.sqlproj`) | **C** existing SQL mechanism |
| EF Core and Dapper/ADO.NET on the same database | **D** mixed: one owner |
| No reliable schema source anywhere | **E** document the gap |

Check that migrations are consistent before trusting them: `dotnet ef migrations list --no-connect -p <migrations
project> -s <startup project>` builds and lists them. Without `--no-connect` it connects to the configured database,
so never omit it here. In audit mode, follow `discovery.md` § 5 instead. `dotnet ef migrations has-pending-model-changes` (EF Core 8+) tells you whether the snapshot matches
the model. Report pending model changes. Do not create migrations to "fix" them unless
asked.

## A. EF Core with existing migrations

Identify the `DbContext` type(s), the migrations assembly (`MigrationsAssembly`, or where `Migrations/` lives), the
startup project that configures the provider and connection key, and any `IDesignTimeDbContextFactory`.

Options, in order of preference:

1. **Existing mechanism.** If the application already migrates on startup in a controlled way (for example a
   dedicated migration job or a guarded `MigrateAsync` behind a flag), reuse it and wire readiness in the AppHost.
2. **Official `AddEFMigrations` integration** (`Aspire.Hosting.EntityFrameworkCore`).
   - Verify availability first: `aspire integration search entityframework` and
     `aspire docs api list csharp/aspire.hosting.entityframeworkcore`.
   - Up to 13.6.x it ships **only as a preview package**, so it needs user consent.
   - Shape:

     ```csharp
     var migrations = api.AddEFMigrations("orders-migrations", "Contoso.Orders.Infrastructure.OrdersDbContext")
         .WithMigrationsProject("../../../src/Contoso.Orders.Infrastructure/Contoso.Orders.Infrastructure.csproj")
         .RunDatabaseUpdateOnStart()
         .WithReference(ordersDb)
         .WaitFor(ordersDb);
     api.WaitForCompletion(migrations);
     ```

   - **Required check after adding it.** With `13.6.3-preview.1.26521.2`, attaching it to the API project made the
     API start without `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT` and `OTEL_EXPORTER_OTLP_ENDPOINT`. As a result it
     listened on the default port, ran as Production (so `/health` was not mapped), was reported Unhealthy and sent no
     telemetry, even though migrations were applied.
   - After adding it, inspect the target process's environment variable **names** (never values): on Linux,
     `tr '\0' '\n' < /proc/<pid>/environ | cut -d= -f1`, or `aspire describe <resource> --format Json` with values
     redacted. If the standard variables are missing, remove the integration and use option 3.
3. **Migration worker** (documented Aspire pattern): a small worker project that applies migrations and exits.
   - Place it under `local/aspire/{App}.MigrationService/` when only local development uses it. If other
     environments also use it as their migration job, it is application code and belongs in `src/`.
   - Reference the migrations/`DbContext` project (and ServiceDefaults for telemetry). Register the `DbContext`
     exactly as the application does (same provider, same connection key).
   - Keep package versions aligned with the existing consumers (see `apphost.md` § Project references).

   ```csharp
   // Program.cs
   var builder = Host.CreateApplicationBuilder(args);
   builder.AddServiceDefaults();
   builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(MigrationWorker.ActivitySourceName));
   builder.Services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("orders")));
   builder.Services.AddHostedService<MigrationWorker>();
   builder.Build().Run();

   // MigrationWorker.cs: apply migrations, then stop so dependants can WaitForCompletion.
   public sealed class MigrationWorker(IServiceProvider services, IHostApplicationLifetime lifetime) : BackgroundService
   {
       public const string ActivitySourceName = "Contoso.Orders.MigrationService";
       private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);
           try
           {
               using var scope = services.CreateScope();
               var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
               await db.Database.CreateExecutionStrategy().ExecuteAsync(() => db.Database.MigrateAsync(stoppingToken));
           }
           catch (Exception ex)
           {
               activity?.AddException(ex);
               throw;
           }

           lifetime.StopApplication();
       }
   }
   ```

   AppHost: `migrations.WithReference(db).WaitFor(db)` and `api.WaitForCompletion(migrations)`. A failure leaves the
   worker with a non-zero exit code and the API waiting. That is what you want: it is visible in `aspire describe`
   and `aspire logs migrations`.

Do not migrate from every application instance at startup, do not use `EnsureCreated` next to migrations, and never
scaffold a new `DbContext` over an existing code-first model.

## B. EF Core with an existing database schema (database-first)

1. Find the authoritative schema: versioned SQL, a `.sqlproj`/DACPAC, an approved schema export, or a sanitized
   snapshot. Never connect to production to discover it.
2. Provision the same engine locally and apply that schema (strategy C mechanics).
3. Reverse engineering a schema into a `DbContext` does not create the database. Only if entities are missing or
   out of date, and the user agrees, run it against the **local** database:

   ```bash
   dotnet ef dbcontext scaffold "<local connection string>" <Provider.Package> \
     -p <data project> -s <startup project> -o <Entities folder> --context <Name>Context \
     --context-dir <folder> --namespace <ns> --no-onconfiguring [--table … | --schema …] [--data-annotations]
   ```

4. Review generated keys, relationships, constraints, nullability, column types, sequences and provider specifics,
   and place the files according to the existing architecture. Do not overwrite customized entities: use partial
   classes or a separate output folder and compare.
5. Decide how the schema is maintained afterwards (SQL stays authoritative and the scaffold is re-run, or move to
   migrations with a baseline) and document it.

## C. Dapper, ADO.NET or direct SQL

- Reuse the existing mechanism: DbUp/FluentMigrator/Evolve runner, `.sqlproj` publish, or ordered scripts.
- If none exists, add the smallest reliable mechanism:
  - versioned scripts `V0001__description.sql` under the canonical location if other environments need them, or
    under `local/database/` if they are local-only;
  - applied in order, each in a transaction where the engine allows it;
  - recorded in a history table so the run is repeatable.
- Prefer a tiny local runner (worker under `local/aspire/`) or the engine's init mechanism. Container init folders
  such as `/docker-entrypoint-initdb.d` run **only on an empty data directory**: fine for first-time init, unsuitable
  for evolving schemas.
- Cover every object the code needs: tables, keys, constraints, indexes, sequences, views, procedures, functions,
  triggers and required reference data. Derive them from SQL in the repository (queries, procedures, repository
  classes), not from DTO property names.
- Verify by running the repository's queries (smoke requests or integration tests) against the initialized database.

## D. Mixed EF Core and Dapper

Pick one owner, usually the existing EF migrations. Make sure the objects Dapper needs (views, procedures, raw tables)
are in that history (`migrationBuilder.Sql(...)`) or in an explicitly ordered step after it. Never let two migration
histories manage the same schema. Verify type mappings (decimal precision, datetime kinds, enums) used by both.

## E. No authoritative schema

Document what is known (tables and columns referenced by code, evidence) and what is missing, provision an empty
database, and stop there for the schema. Ask for an approved source. Never invent a production-like schema.

## Persistence

- Databases that developers keep data in: `.WithDataVolume()` plus `.WithLifetime(ContainerLifetime.Persistent)`.
  A persistent container alone does not persist data, and configuration changes recreate it.
- Volume names are generated from the AppHost and resource name. Verify the actual names with
  `docker volume ls`; in 13.6.x they look like `<apphost>-<hash>-<resource>-data`.
- The generated server password is stored in AppHost user secrets on the first run, and existing volumes need the
  same password. If user secrets are lost, the database container cannot log in to its old data. Pin the password
  with a secret parameter when that matters, and document it.
- `AddDatabase("name")` creates the database when it is missing on a supported engine (verified with PostgreSQL in
  13.6.x). Older versions or other engines may not, so check.

## Safety

| Operation | Effect | Rule |
|---|---|---|
| `aspire stop` | Stops the AppHost. Persistent containers keep running, volumes stay. | Safe. Only stop AppHosts you started. |
| `aspire stop --force` | Also removes persistent containers. Volumes stay. | Ask first. |
| `aspire stop --force --volumes` | **Deletes Aspire-created volumes with no confirmation prompt.** | Only on explicit request, after naming the volumes. |
| `docker volume rm`, `dotnet ef database drop`, `DROP DATABASE` | Data loss | Only on explicit request. |
| Resource names, AppHost path or rename | New volume names: old data no longer attached | Warn before doing it. |

Ordinary startup never resets data. Document a separate, explicit reset procedure in `local/local-development.md`
(stop, list the volumes, delete only the named local volume, start, migrations re-run), and put it in a
`local/scripts/` script that prompts for confirmation if the repository uses scripts. Seed data must be synthetic or
approved for development, never copied from production.

## Fresh-database verification

To prove initialization works from scratch without touching the developer's data, point the database resource at a
temporary database name, or start with `aspire start --isolated` when the integration isolates storage (check the
installed version's behaviour). Then confirm:

- the initializer finished with exit code 0 (`aspire describe` shows `Finished`, `exitCode` 0);
- the expected migration or script list was applied (`aspire logs <initializer>`);
- a smoke request that reads and writes succeeds.

Revert the temporary name afterwards and remove only the temporary database or volume you created.
