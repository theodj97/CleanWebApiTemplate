# Databases: strategy, initializer, seed, ephemeral data, test resets

Canonical for SKILL.md rule 7 (ephemeral data and data safety) and for every database rule of local development and
tests. Templates: `assets/tests/shared/Database/*`, `assets/tests/shared/Attributes/ResetDatabaseAttribute.cs`,
`assets/tests/shared/Data/*`, resetter wiring in `assets/tests/shared/Fixtures/TestServerFixtureBase.cs`.

## Contents
- [Ephemeral by design](#ephemeral-by-design)
- [Choosing the schema strategy](#choosing-the-schema-strategy)
- [The database initializer](#the-database-initializer)
- [A. EF Core with existing migrations](#a-ef-core-with-existing-migrations)
- [B. EF Core with an existing database schema](#b-ef-core-with-an-existing-database-schema-database-first)
- [C. Dapper, ADO.NET or direct SQL](#c-dapper-adonet-or-direct-sql)
- [D. Mixed EF Core and Dapper](#d-mixed-ef-core-and-dapper)
- [E. No authoritative schema](#e-no-authoritative-schema)
- [Seed data (Local mode)](#seed-data-local-mode)
- [Safety](#safety)
- [Verification in Local mode](#verification-in-local-mode)
- [Tests: one database per engine per run](#tests-one-database-per-engine-per-run)
- [Schema in test modes](#schema-in-test-modes)
- [ResetDatabase semantics](#resetdatabase-semantics)
- [Resetters](#resetters)
- [Respawn configuration per engine](#respawn-configuration-per-engine)
- [Engines Respawn does not support](#engines-respawn-does-not-support)
- [Data that must survive a reset](#data-that-must-survive-a-reset)
- [Test repositories](#test-repositories)
- [Builders and Bogus](#builders-and-bogus)

## Ephemeral by design

**Every AppHost start, in every mode, builds each database from scratch.** The order is fixed:

1. create the container and the database;
2. apply the schema with the authoritative mechanism (migrations or scripts);
3. Local mode only: load synthetic seed data with the SQL scripts of `local/database/seed/`;
4. only then start the applications (`WaitForCompletion`).

Nothing survives between runs. Every developer, agent and test run starts from a known state, and every start proves
that the schema can be built from its source.

- **Forbidden on local data resources** (databases, caches, brokers, storage emulators), in every mode, no opt-in, no
  exceptions: `WithDataVolume`, `WithDataBindMount`, `WithLifetime(ContainerLifetime.Persistent)`,
  `WithPersistentLifetime`.
- **Default lifetime: session.** Containers are created when the AppHost starts and removed when it stops, together
  with their data. Clean-up is asynchronous: in 13.6.x the container and its anonymous volume disappear a few seconds
  after `aspire stop` returns; wait before concluding something was left behind.
- **Passwords:** generated credentials only need to be valid for the current run: do not pin database passwords.
  Secret parameters remain the way to provide external credentials.
- **Database creation:** `AddDatabase("name")` creates the database on every start (verified with PostgreSQL in
  13.6.x; SQL Server and MySQL also create it in recent versions). If an engine does not, the initializer creates it
  before migrating (EF `MigrateAsync` creates it).
- **Code changes:** changing `AppHost.cs` needs an AppHost restart, which rebuilds the data, as designed. Restarting one
  application resource (`aspire resource <app> restart`) keeps the current database.
- What *does* persist across restarts is outside the containers: the local JWT key in the AppHost user secrets
  (`local-jwt-authentication.md` § Key storage) and the seed scripts in the repository. Data a developer needs again
  goes into a seed script.
- **Existing AppHosts that persist data** (repair): remove the volume and lifetime calls from the model, list the
  volumes they created (`docker volume ls`, usually `<apphost>-<hash>-<resource>-data`) and persistent containers by
  name, and leave them in place with instructions to remove them: they may hold data the developer still wants.

## Choosing the schema strategy

For each database choose **one** authoritative schema-management strategy. Initialization runs once per AppHost start
(never once per application instance) and finishes before anything that needs the database starts.

| Evidence | Strategy |
|---|---|
| `DbContext` + a consistent `Migrations/` folder with a model snapshot | **A** EF Core migrations |
| EF Core used, but no migrations (or entities clearly reverse-engineered), schema owned by SQL/DBA | **B** EF Core database-first |
| Dapper / ADO.NET / stored procedures, with SQL scripts or a migration tool (DbUp, FluentMigrator, Evolve, Flyway, Liquibase, `.sqlproj`) | **C** existing SQL mechanism |
| EF Core and Dapper/ADO.NET on the same database | **D** mixed: one owner |
| No reliable schema source anywhere | **E** document the gap |

Check that migrations are consistent before trusting them:

- `dotnet ef migrations list --no-connect -p <migrations project> -s <startup project>` builds and lists them. Without
  `--no-connect` it connects to the configured database, so never omit it here. When the startup project cannot be
  used at design time (it reads secrets eagerly), use an `IDesignTimeDbContextFactory` in the migrations project and
  pass it as startup.
- `dotnet ef migrations has-pending-model-changes` (EF Core 8+) tells you whether the snapshot matches the model.
- In audit mode follow `discovery.md` § 5 instead (no builds).

Report pending model changes. Do not create migrations to "fix" them unless asked. `EnsureCreated` next to migrations
is a defect to report: they are incompatible.

## The database initializer

One small worker project, `local/aspire/{App}.DbInitializer/` (keep the name `{App}.MigrationService` if the repository
already has one), is the single schema mechanism of **all three modes** (*changed:* the former testing skill applied
the schema from the test fixture). It does, in order, then exits:

1. **Schema**: `MigrateAsync` (A, D), the schema scripts / migration tool (B, C), or `EnsureCreatedAsync` when the
   application is EF Core code-first without migrations and creates its schema with `EnsureCreated` (report that the
   model is then the only schema source and cannot evolve an existing database).
2. **Seed**, only when the AppHost passes `Seed__Path` (Local mode): every `*.sql` in `local/database/seed/`, in
   ordinal file-name order, each in its own transaction.

Then `StopApplication()`, so dependants use `WaitForCompletion`. A failure leaves a non-zero exit code and the
dependants waiting, visible in `aspire describe`, `aspire logs <initializer>` and, in tests, in the fixture's
`WaitForSuccessfulCompletionAsync` error.

Rules:

- It lives under `local/aspire/` because it is local-only. If other environments use the same worker as their
  migration job, it is application code: keep it in `src/` and put the seed step behind configuration only the AppHost
  sets.
- Reference the migrations/`DbContext` project (and ServiceDefaults for telemetry). Register the `DbContext` exactly as
  the application does: same provider, same connection key.
- Keep package versions aligned with the existing consumers (`architecture-and-layout.md` § Project references).
- The AppHost passes the seed folder, so no files are copied:
  `.WithEnvironment("Seed__Path", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../database/seed")))`.

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(DbInitializer.ActivitySourceName));
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));
builder.Services.AddHostedService<DbInitializer>();
builder.Build().Run();

// DbInitializer.cs: builds the database from scratch on every AppHost start: schema, then (Local mode) seed scripts.
public sealed class DbInitializer(
    IServiceProvider services, IConfiguration configuration, IHostApplicationLifetime lifetime,
    ILogger<DbInitializer> logger) : BackgroundService
{
    public const string ActivitySourceName = "Contoso.Orders.DbInitializer";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Initializing database", ActivityKind.Client);
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(() => db.Database.MigrateAsync(stoppingToken));
            logger.LogInformation("Applied migrations: {Migrations}",
                string.Join(", ", await db.Database.GetAppliedMigrationsAsync(stoppingToken)));
            await strategy.ExecuteAsync(() => SeedAsync(db, stoppingToken));
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        lifetime.StopApplication();
    }

    private async Task SeedAsync(OrdersDbContext db, CancellationToken cancellationToken)
    {
        var seedPath = configuration["Seed:Path"];
        if (string.IsNullOrEmpty(seedPath) || !Directory.Exists(seedPath))
        {
            logger.LogInformation("No seed directory configured; skipping seed");   // test modes
            return;
        }

        foreach (var script in Directory.GetFiles(seedPath, "*.sql").Order(StringComparer.Ordinal))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(script, cancellationToken), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Applied seed script {Script}", Path.GetFileName(script));
        }
    }
}
```

Without EF Core (strategy C), run the scripts with the ADO.NET client the repository already uses (`NpgsqlConnection`,
`SqlConnection`, …), or invoke the existing migration tool (DbUp, FluentMigrator…) from the same worker before the
seed step.

### When the application migrates itself

If the application already applies its schema on startup (`Database.Migrate()` in `Program.cs` or a hosted service),
never add a second migration runner next to it:

- If the application has a configuration switch for it, turn its startup migration off in every AppHost mode
  (configuration only) and let the initializer own the schema as above.
- If it is unconditional, the initializer has no schema step (remove `MigrateAsync`): in Local mode it only runs the
  seed and waits for the application instead (`dbInitializer.WaitFor(app)`, and the app no longer
  `WaitForCompletion(dbInitializer)`); in test modes it is not needed at all (the fixtures set
  `SchemaInitializerResource => null` and `SchemaAppliedByApplication => true`, § Schema in test modes).
- Either way, report "migrations run from every application instance at startup" as a finding (§ A); changing it is a
  production change that needs consent.

## A. EF Core with existing migrations

Identify the `DbContext` type(s), the migrations assembly (`MigrationsAssembly`, or where `Migrations/` lives), the
startup project that configures the provider and connection key, and any `IDesignTimeDbContextFactory`. Options, in
order of preference:

1. **The initializer** above: `MigrateAsync`, then (Local) seed. It reuses the full migration history every run.
2. **An existing controlled mechanism** (a dedicated migration job, or a guarded `MigrateAsync` behind a flag): reuse
   it for the schema step, wire readiness in the AppHost, and still run the seed separately afterwards.
3. **Official `AddEFMigrations`** (`Aspire.Hosting.EntityFrameworkCore`): only with consent, always with a separate
   seed step.
   - Up to 13.6.x it ships **only as a preview**. Verify with `aspire integration search entityframework` and
     `aspire docs api list csharp/aspire.hosting.entityframeworkcore`.
   - Shape: `api.AddEFMigrations("orders-migrations", "<Namespace>.OrdersDbContext").WithMigrationsProject("<csproj>").RunDatabaseUpdateOnStart()`
     plus `api.WaitForCompletion(migrations)`.
   - **Known defect.** With `13.6.3-preview.1.26521.2`, attached to the API project, the API started without
     `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT` and `OTEL_EXPORTER_OTLP_ENDPOINT` (default port, Production, no
     `/health`, Unhealthy, no telemetry). If you use it, inspect the target's environment variable **names** (never
     values): `tr '\0' '\n' < /proc/<pid>/environ | cut -d= -f1`, or `aspire describe <resource> --format Json`
     redacted. If the standard variables are missing, remove it and use option 1.

Do not migrate from every application instance at startup, do not use `EnsureCreated` next to migrations, never
scaffold a new `DbContext` over an existing code-first model. `HasData` seeding is part of the schema (migrations),
not of the local seed.

## B. EF Core with an existing database schema (database-first)

1. Find the authoritative schema: versioned SQL, a `.sqlproj`/DACPAC, an approved schema export, or a sanitized
   snapshot. Never connect to production to discover it.
2. Apply it in the initializer's schema step on every start (strategy C mechanics). For a `.sqlproj`/DACPAC: build the
   database project first (a `ProjectReference` with `ReferenceOutputAssembly="false"` from the initializer, or a build
   step), then deploy it with `new DacServices(cs).Deploy(DacPackage.Load(path), dbName, upgradeExisting: true)`
   (`Microsoft.SqlServer.DacFx`).
3. Reverse engineering a schema into a `DbContext` does not create the database. Only if entities are missing or out
   of date, and the user agrees, run it against the **local** database:
   ```bash
   dotnet ef dbcontext scaffold "<local connection string>" <Provider.Package> \
     -p <data project> -s <startup project> -o <Entities folder> --context <Name>Context \
     --context-dir <folder> --namespace <ns> --no-onconfiguring [--table … | --schema …] [--data-annotations]
   ```
4. Review keys, relationships, constraints, nullability, column types, sequences and provider specifics; place the
   files according to the existing architecture; never overwrite customized entities (partial classes or a separate
   output folder, then compare).
5. Decide how the schema is maintained afterwards (SQL stays authoritative and the scaffold is re-run, or move to
   migrations with a baseline) and document it.

## C. Dapper, ADO.NET or direct SQL

- Reuse the existing mechanism (DbUp/FluentMigrator/Evolve runner, `.sqlproj` publish, ordered scripts) as the
  initializer's schema step. DbUp: call the application's upgrade method or replicate its `DeployChanges.To…`
  configuration with the same script source and fail on `!result.Successful`. FluentMigrator:
  `AddFluentMigratorCore().ConfigureRunner(r => r.Add{Engine}().WithGlobalConnectionString(cs).ScanIn(typeof(SomeMigration).Assembly).For.Migrations())`
  and `IMigrationRunner.MigrateUp()`.
- If none exists, add the smallest reliable mechanism: versioned scripts `V0001__description.sql`, in the canonical
  location if other environments need them, otherwise in `local/database/schema/`; applied in order, each in a
  transaction where the engine allows it; a history table only if other environments need incremental upgrades.
- Container init folders (`/docker-entrypoint-initdb.d`, `WithInitFiles`, `WithCreationScript`) run on every start of a
  new, empty container, so they fit this model. Use them only without EF migrations and when they guarantee schema
  first, then seed; otherwise prefer the initializer, so ordering and failures are explicit.
- Cover every object the code needs: tables, keys, constraints, indexes, sequences, views, procedures, functions,
  triggers and required reference data, derived from the SQL in the repository, not from DTO property names.
- Verify by running the repository's queries (smoke requests or tests) against the initialized database.

## D. Mixed EF Core and Dapper

Pick one owner, usually the existing EF migrations. The objects Dapper needs (views, procedures, raw tables) are in
that history (`migrationBuilder.Sql(...)`) or in an explicitly ordered schema step after it, never in the seed. Never
let two migration histories manage the same schema. Verify type mappings (decimal precision, datetime kinds, enums)
used by both.

## E. No authoritative schema

Document what is known (tables and columns the code references, with evidence) and what is missing, provision an
empty database, and stop there for the schema. Ask for an approved source. Never invent a production-like schema, do
not write seed scripts for a schema that does not exist. If the user approves an inferred schema for tests and local
use, write it to `local/database/schema/schema.sql` from the entities, mappings and SQL in the code, say it is
inferred and must be reviewed, and ask before relying on it.

## Seed data (Local mode)

- **Location:** `local/database/seed/NNN_description.sql` (`001_reference_data.sql`, `002_sample_orders.sql`…), run in
  ordinal name order after the schema. One transaction per script. Test modes never run it.
- **Content:** the reference data the application needs to start (lookups, default tenants or roles), then synthetic
  sample data for the main flows; synthetic or approved for development only, never copied from production, no real
  personal data or secrets. Synthetic local passwords for seeded users are allowed and documented as local-only.
- **Write it for an empty database:** no `IF NOT EXISTS` guards, no history table; rely on generated identity and
  sequence values where possible, or set sequences after inserting fixed ids.
- **Same SQL dialect as the engine**, identifiers quoted as the schema created them (`"Orders"` for EF Core on
  PostgreSQL).
- **Reuse what exists:** existing local/dev seed data (SQL, JSON fixtures, a seeding command) is used or converted,
  never duplicated in a parallel set.
- **Align with devtoken profiles** when users live in the database: seeded user ids can equal profile subjects.
- **No seed needed?** If the application works with empty tables and the user wants no sample data, leave the folder
  out and say so: the initializer skips a missing folder.

## Safety

| Operation | Rule |
|---|---|
| Removing persistence from an existing AppHost | Allowed: it changes the model only. Report orphaned volumes and containers by name; do not delete them. |
| `docker volume rm`, `docker rm` of pre-existing resources, `aspire stop --force [--volumes]` | Only on explicit request, after naming exactly what will be removed. `--force --volumes` deletes with no prompt. |
| `dotnet ef database drop`, `DROP DATABASE` against anything but the AppHost's own containers | Never. |
| Connecting to shared, staging or production databases | Never, not even to discover a schema. |
| Stopping AppHosts | Only the ones you started (`aspire ps` before and after). |
| Destructive test resets | Only against the AppHost's local containers (`DatabaseResetTarget`). |

## Verification in Local mode

Every start is a fresh-database start, so verify it on every validation:

- the initializer finished with exit code 0 (`aspire describe` shows `Finished`, `exitCode` 0);
- its logs list the applied migrations or schema scripts, then each seed script (`aspire logs <initializer>`);
- a smoke request returns the seed data and can write;
- **two-run check**: write a record, `aspire stop`, `aspire start` again: the record is gone, the seed data is back;
- `docker volume ls` / `docker ps -a` show nothing left by the AppHost once clean-up has finished.

## Tests: one database per engine per run

Each engine runs in one Aspire container per test session with the databases the application uses, created when the
fixture starts the AppHost. Never create a database (or a container) per test or per class: creating schemas is slow
and buys nothing over deleting rows. Between tests only data is deleted.

Never replace the engine with SQLite or EF Core InMemory, and never a broker with an in-memory transport: they differ in
transactions, types, collations, constraints and SQL dialect and hide real bugs.

## Schema in test modes

- **Default:** the shared initializer applies the schema (no seed) and the fixture waits for it to finish with exit
  code 0 before it builds test services, starts the application and creates the resetters
  (`TestServerFixtureBase.WaitForSuccessfulCompletionAsync`). Functional apps also `WaitForCompletion` on it.
- **The application applies its schema on startup** and it cannot be switched off (§ When the application migrates
  itself): set `SchemaInitializerResource => null` and `SchemaAppliedByApplication => true`; after starting the
  application the fixture polls `GetPendingMigrationsAsync()` until it is empty before creating the resetters
  ("started" does not mean "migrated"). Component mode then has no schema before the in-process app starts, which is
  exactly when the app applies it.
- Same mechanism, options, migrations assembly and history table as production: the tests exercise the real schema.

## ResetDatabase semantics

- `[ResetDatabase]` resets every registered database; `[ResetDatabase("OrdersDb")]` or
  `[ResetDatabase("OrdersDb", "AuditDb")]` only those; `ExcludeTables = ["Countries"]` keeps extra tables for that test.
- `ExcludeTables` means "extra things to keep" for every engine: table names for Respawn, collection names for
  MongoDB, key prefixes for Redis.
- On the method or the class; the method attribute wins.
- The attribute only declares intent: `TestBase.InitializeAsync` reads it and awaits the resetters (attribute hooks are
  synchronous; blocking async I/O inside xUnit deadlocks).
- **Before the test, never after.** Every test starts from a known state even if the previous run was interrupted, and
  a failed test keeps its data for inspection.
- Tests without the attribute see whatever previous tests left: use it on every test that writes or that asserts on
  counts or absence.
- Tests never depend on concrete id values: identity columns and sequences may or may not restart. Use the ids the
  test repositories return.

## Resetters

`IDatabaseResetter` (`Name`, `ResetAsync(extraTablesToKeep, ct)`), one implementation per engine family, all registered
in `Fixture.Databases` (`DatabaseResetters`). For every implementation:

- delete rows, never structure (tables, indexes, collections, sequences);
- if constraints or triggers must be disabled, re-enable them in a `finally` block;
- what survives (SQL) and what is reset (MongoDB, Redis) lives in one place: `DatabaseResetConfiguration`;
- destructive operations are allowlisted, never "everything except the keep list": MongoDB and Redis empty only the
  configured collections and key prefixes (enumerating collections or `FLUSHDB` would destroy data discovery never
  saw);
- the MongoDB and Redis resetters refuse non-local hosts (`DatabaseResetTarget`): the AppHost publishes containers on
  loopback, so a remote host means a wrong connection string. If Docker really runs elsewhere, set
  `DOTNET_TESTING_ALLOW_REMOTE_DATABASE_RESET=1` explicitly and accept that the guard is off.

## Respawn configuration per engine

`RespawnDatabaseResetter` wraps Respawn (template verified with Respawn 7.0.0) for SQL Server, PostgreSQL,
MySQL/MariaDB and Oracle.

- Create one `Respawner` per database **once**, after the schema exists: it reads and caches the table graph.
- Pass an open `DbConnection` (required for every engine except SQL Server, harmless there).
- `RespawnerOptions` are immutable once the `Respawner` exists, so a test's `ExcludeTables` needs a variant `Respawner`,
  created on first use and cached.

| Engine | Options |
|---|---|
| SQL Server | `DbAdapter = DbAdapter.SqlServer`, `TablesToIgnore`, `WithReseed = true`, `CheckTemporalTables = true` with system-versioned tables |
| PostgreSQL | `DbAdapter = DbAdapter.Postgres`, `SchemasToInclude = ["public", …every schema the app uses]`, `TablesToIgnore`, `WithReseed = true` |
| MySQL / MariaDB | `DbAdapter = DbAdapter.MySql`, `SchemasToInclude = [databaseName]`, `TablesToIgnore` |
| Oracle | `DbAdapter = DbAdapter.Oracle`, `SchemasToInclude = [SCHEMA_OWNER]` (upper case), `TablesToIgnore` |

`TablesToIgnore` (`new Table("schema", "name")` or `"name"`) always contains the migration history table
(`__EFMigrationsHistory`, DbUp `SchemaVersions`, FluentMigrator `VersionInfo`, or the custom name found) and the
reference tables seeded with the schema that the application only reads. `WithReseed` only where the adapter supports
it (SQL Server, PostgreSQL). PostgreSQL caveat: Respawn uses `TRUNCATE … CASCADE`, so an ignored table with a foreign
key to a reset table is emptied too: keep reference tables free of such keys or treat them as surviving data.

## Engines Respawn does not support

| Engine | Resetter |
|---|---|
| MongoDB | `MongoDatabaseResetter`: `DeleteMany` on `DatabaseResetConfiguration.MongoCollectionsToReset`. Keeps collections and indexes. |
| Redis | `RedisDatabaseResetter`: `SCAN` + `DEL` under `RedisKeyPrefixesToReset`, in the logical database the app uses. No `FLUSHDB`, no `allowAdmin=true`. |
| Cosmos DB (emulator) | delete the items of each container (query ids + partition keys, batches); never delete containers. |
| Azure Storage (Azurite) | delete blobs in the app's containers, clear queues, delete table entities. |

Both template resetters throw at startup when their allowlist is empty, on purpose. Redis keys without a stable prefix
(e.g. `AddStackExchangeRedisCache` with an empty `InstanceName`) have no safe allowlist: give the test modes a dedicated
instance name or prefix in the AppHost instead of deleting the whole database.

## Data that must survive a reset

When the application needs rows to exist (test users linked by `sub`, a tenant row, configuration rows it also writes),
do not put those tables in `TablesToIgnore`: let them be reset and re-insert the rows in `SeedPersistentDataAsync`,
which the fixture calls once at startup and `TestBase` after every reset. Make it idempotent (insert-if-missing or
upsert). For test users, upsert `TestUsers.All` with the same subjects the tokens carry. MongoDB and Redis: a
collection or prefix that must survive is simply not listed; for one test only, pass it in `ExcludeTables`.

## Test repositories

One `{Entity}TestRepository` per aggregate or table the tests arrange or assert on, in Shared, exposed as fixture
properties (`Fixture.Orders`, `Fixture.Users`).

- `AddAsync(entity)` inserts and returns the entity with its generated id (Arrange); query methods for Assert
  (`FindAsync`, `GetSingleBy…Async`, `CountAsync`), named after what they check.
- EF apps: resolve the application's `DbContext` from the test-side services, one fresh scope per call, `AsNoTracking`
  for queries. Component tests use the test services too, not the application's container.
- Apps without EF: the same shape with Dapper or ADO.NET over the container connection string.
- No `CancellationToken` parameter: repositories use `TestCancellation.Token`.
- Repositories bypass the application on purpose for Arrange and Assert; never use them to perform the Act.

## Builders and Bogus

- One builder per entity with **valid defaults**; each test states only what matters
  (`new OrderBuilder().ForCustomer(subject).WithAmount(100m).Build()`). Use the domain's constructor or factory so
  invariants stay enforced.
- Bogus always with a fixed seed (`TestData.CreateFaker()`); `PrepareForTestAsync` reseeds per test from a stable hash
  of the test name, so data does not depend on execution order. Unit tests seed their own Faker with a constant.
- Unique columns: combine Faker output with `TestData.Unique("prefix")`.
- Related ids come from inserted entities or `TestUsers`, never from literals.
