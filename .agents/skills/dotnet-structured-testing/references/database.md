# Database: schema, reset with Respawn, test repositories and data

Templates: `assets/shared/Database/*`, `assets/shared/Attributes/ResetDatabaseAttribute.cs`, `assets/shared/Data/*`,
schema/resetter wiring in `assets/shared/Fixtures/TestServerFixtureBase.cs`.

## Contents
- [One database per engine per run](#one-database-per-engine-per-run)
- [Building the schema](#building-the-schema)
- [ResetDatabase semantics](#resetdatabase-semantics)
- [Resetters](#resetters)
- [Respawn configuration per engine](#respawn-configuration-per-engine)
- [Engines Respawn does not support](#engines-respawn-does-not-support)
- [Data that must survive a reset](#data-that-must-survive-a-reset)
- [Test repositories](#test-repositories)
- [Builders and Bogus](#builders-and-bogus)

## One database per engine per run

Each engine runs in one Aspire container per test session with the databases the app uses, created when the fixture
starts. Never create a database per test or per class: creating schemas is slow and buys nothing over deleting rows.
Between tests only data is deleted.

Never replace the engine with SQLite or EF Core InMemory: they behave differently (transactions, types, collations,
constraints, SQL dialect) and hide real bugs. Use the same engine as production, in a container.

Aspire creates the databases declared with `AddDatabase(...)` on SQL Server, PostgreSQL and MySQL when the server
starts (recent versions); otherwise the schema mechanism creates them (EF `MigrateAsync` creates the database).

## Building the schema

Use the same mechanism as production so the tests exercise the real schema:

| Strategy found in discovery | In the fixture (`ApplySchemaAsync`) |
|---|---|
| EF Core migrations | `await db.Database.MigrateAsync(ct)` on a DbContext from the test-side services (same options, migrations assembly and history table as production). When the app migrates on startup (`SchemaAppliedByApplication => true`) skip it, and the fixture polls `GetPendingMigrationsAsync()` until it is empty before creating the resetters — "started" does not mean "migrated". |
| EF without migrations | `await db.Database.EnsureCreatedAsync(ct)` |
| DbUp | call the app's upgrade method if it exposes one; otherwise replicate its `DeployChanges.To...` configuration with the same script source, against the container connection string, and fail on `!result.Successful` |
| FluentMigrator | build a service provider with `AddFluentMigratorCore().ConfigureRunner(r => r.Add{Engine}().WithGlobalConnectionString(cs).ScanIn(typeof(SomeMigration).Assembly).For.Migrations())` and call `IMigrationRunner.MigrateUp()` |
| `.sqlproj` / DACPAC | build the database project (ProjectReference with `ReferenceOutputAssembly="false"` from Shared, or a build step), then deploy with `Microsoft.SqlServer.DacFx`: `new DacServices(cs).Deploy(DacPackage.Load(path), dbName, upgradeExisting: true)` |
| Scripts | run them in their documented order with the engine's client, or attach them in the AppHost (`WithCreationScript` on the database resource, or init scripts mounted for PostgreSQL/MySQL images) |
| None | generate `tests/orchestration/database/schema.sql` from the entities, the mappings and the SQL found in the code; tell the user it is inferred and must be reviewed; ask before relying on it |

Functional mode: the fixture applies the schema before starting the app (the app resource uses `WithExplicitStart`).
If explicit start is not available in the repository's Aspire version and the app needs the schema at startup, add a
tiny migrator console project under `tests/orchestration/` that applies the schema, declare it as a resource, and make
the app `WaitForCompletion(migrator)`.

## ResetDatabase semantics

- `[ResetDatabase]` resets every registered database; `[ResetDatabase("OrdersDb")]` or
  `[ResetDatabase("OrdersDb", "AuditDb")]` only those; `ExcludeTables = ["Countries"]` keeps extra tables for that test.
- `ExcludeTables` is named after SQL but means "extra things to keep" for every engine: table names for Respawn,
  collection names for MongoDB and key prefixes for Redis.
- On the method or on the class; on the class it applies to every test, and the method attribute wins.
- The attribute only declares intent: `TestBase.InitializeAsync` reads it and awaits the resetters (attribute hooks are
  synchronous; blocking async I/O inside xUnit deadlocks).
- **Before the test, never after.** Every test starts from a known state even if the previous run was interrupted,
  and a failed test leaves its data behind for inspection.
- Tests without the attribute see whatever previous tests left: use it on every test that writes or that asserts on
  counts/absence.
- Tests never depend on concrete id values: identity columns and sequences may or may not restart. Use the ids the
  test repositories return.

## Resetters

`IDatabaseResetter` (`Name`, `ResetAsync(extraTablesToKeep, ct)`) with one implementation per engine family, all
registered in `Fixture.Databases` (`DatabaseResetters`). Rules for every implementation:

- Delete rows, never structure (tables, indexes, collections, sequences).
- If an implementation must disable constraints or triggers, re-enable them in a `finally` block: a failure halfway
  must not leave the database without its constraints for the rest of the run.
- The list of what survives (SQL) and what is reset (MongoDB, Redis) lives in one place: `DatabaseResetConfiguration`
  (from discovery section 3).
- Destructive operations are allowlisted, never "everything except the keep list". For MongoDB and Redis that means
  emptying only the configured collections and deleting only the configured key prefixes: enumerating collections or
  running `FLUSHDB` would also destroy data the app creates but discovery never saw.
- The MongoDB and Redis resetters refuse to run against a non-local host (`DatabaseResetTarget`): the AppHost publishes
  its containers on the loopback interface, so a remote host means a wrong connection string. If Docker really runs on
  another machine, set `DOTNET_TESTING_ALLOW_REMOTE_DATABASE_RESET=1` explicitly and accept that the guard is off.

## Respawn configuration per engine

`RespawnDatabaseResetter` wraps Respawn for SQL Server, PostgreSQL, MySQL/MariaDB and Oracle.

- Create one `Respawner` per database **once**, after the schema exists: Respawn reads the table graph (foreign keys,
  delete order) when the `Respawner` is created, and caches it. The resetter is created in the fixture and kept there.
- Pass an open `DbConnection` (required for every engine except SQL Server, harmless for SQL Server).
- `RespawnerOptions` are immutable once the `Respawner` exists, so a test's `ExcludeTables` needs a variant `Respawner`:
  the template creates one per distinct table set on first use and caches it.

| Engine | Options |
|---|---|
| SQL Server | `DbAdapter = DbAdapter.SqlServer`, `TablesToIgnore`, `WithReseed = true` (DBCC CHECKIDENT), `CheckTemporalTables = true` if the app uses system-versioned tables |
| PostgreSQL | `DbAdapter = DbAdapter.Postgres`, `SchemasToInclude = ["public", …every schema the app uses]`, `TablesToIgnore`, `WithReseed = true` (restarts identity/serial sequences) |
| MySQL / MariaDB | `DbAdapter = DbAdapter.MySql`, `SchemasToInclude = [databaseName]`, `TablesToIgnore` |
| Oracle | `DbAdapter = DbAdapter.Oracle`, `SchemasToInclude = [SCHEMA_OWNER]` (upper case), `TablesToIgnore` |

`TablesToIgnore` takes `new Table("schema", "name")` (or just `"name"`). It always contains the migration history table
(`__EFMigrationsHistory`, DbUp `SchemaVersions`, FluentMigrator `VersionInfo`, or the custom name found) and the
reference tables seeded with the schema that the app only reads. Enable `WithReseed` only where the adapter supports it
(SQL Server, PostgreSQL); elsewhere leave it off — tests never depend on id values anyway.

PostgreSQL caveat: Respawn uses `TRUNCATE ... CASCADE`, so an ignored table with a foreign key to a reset table is
emptied too. Keep reference tables free of foreign keys to transactional tables, or treat them as surviving data
(re-seeded after reset) instead of ignored tables.

## Engines Respawn does not support

| Engine | Resetter |
|---|---|
| MongoDB | `MongoDatabaseResetter`: `DeleteMany` on the collections listed in `DatabaseResetConfiguration.MongoCollectionsToReset`. Keeps collections and indexes (dropping would also drop indexes the app created at startup). |
| Redis | `RedisDatabaseResetter`: `SCAN` + `DEL` of the keys under `DatabaseResetConfiguration.RedisKeyPrefixesToReset`, in the logical database the app uses. No `FLUSHDB` and no `allowAdmin=true`; keys outside those prefixes survive. |
| Cosmos DB (emulator) | delete the items of each container (query ids + partition keys, delete in batches); never delete containers. |
| Azure Storage (Azurite) | delete blobs in the app's containers, clear queues, delete table entities. |

Register them in the fixture like the Respawn one, passing the allowlist from the configuration:

```csharp
Databases.Add(new MongoDatabaseResetter(
    DatabaseResetConfiguration.Names.Catalog,
    catalogDatabase,
    DatabaseResetConfiguration.MongoCollectionsToReset(DatabaseResetConfiguration.Names.Catalog)));

Databases.Add(new RedisDatabaseResetter(
    DatabaseResetConfiguration.Names.Catalog,
    redis,
    DatabaseResetConfiguration.RedisKeyPrefixesToReset(DatabaseResetConfiguration.Names.Catalog),
    database: 0));
```

Both throw at fixture startup when the allowlist is empty. That is intentional: tests must not silently skip the reset
because discovery missed a collection or a key prefix.

Redis keys without a stable prefix (for example `AddStackExchangeRedisCache` with an empty `InstanceName`) have no safe
allowlist: give the test environment a dedicated instance name/prefix in the AppHost instead of falling back to
"delete the whole database".

## Data that must survive a reset

When discovery finds that the app needs rows to exist (test users linked by `sub`, a tenant row, configuration rows the
app writes too), do not put those tables in `TablesToIgnore` — tests write to them as well and leftovers would leak
between tests. Instead let them be reset and re-insert the required rows in `SeedPersistentDataAsync`, which the
fixture calls once at startup (a first test without `[ResetDatabase]` needs them too) and `TestBase` calls after every
reset. Make it idempotent (insert-if-missing or upsert): a reset of another database, or `ExcludeTables` containing
that table, leaves the rows in place. For test users, upsert `TestUsers.All` through a test repository, with the same
subject values the tokens carry.

MongoDB and Redis use allowlists instead: a collection or key prefix that must survive is simply not listed in
`MongoCollectionsToReset` / `RedisKeyPrefixesToReset`. If only one test must preserve it, pass the collection name or key
prefix in `ExcludeTables` for that test.

## Test repositories

One `{Entity}TestRepository` per aggregate or table the tests arrange or assert on, in Shared, exposed as fixture
properties (`Fixture.Orders`, `Fixture.Customers`).

- `AddAsync(entity)` inserts and returns the entity with its generated id (Arrange).
- Query methods for Assert: `FindAsync(id)`, `GetSingleBy...Async`, `CountAsync`, `ExistsAsync` — whatever the tests
  need, named after what they check.
- EF apps: resolve the app's DbContext from the test-side services (`IServiceScopeFactory`), one fresh scope per call
  (never see stale tracked entities), `AsNoTracking` for queries. In component tests the DbContext comes from the test
  services too, not from the app's container, so repositories behave the same in both projects.
- Apps without EF: same shape with Dapper or plain ADO.NET over the container connection string, with the SQL written
  against the real schema.
- No `CancellationToken` parameter: repositories use `TestCancellation.Token` internally.
- Repositories bypass the application on purpose (Arrange without going through the endpoints under test); never use
  them to perform the Act.

## Builders and Bogus

- One builder per entity with **valid defaults**, so each test only states the values that matter
  (`new OrderBuilder().ForCustomer(customer.Id).WithAmount(100m).Build()`). Use the domain's constructor or factory
  so invariants keep being enforced.
- Bogus always with a fixed seed (`TestData.CreateFaker()`): the same test produces the same data on every run.
  The fixture's `PrepareForTestAsync` reseeds per test from a stable hash of the test name, so data does not depend on
  execution order. Unit tests, which run in parallel and do not reference Shared, seed their own Faker with a constant.
- Unique columns (emails, codes, names with unique indexes): combine Faker output with `TestData.Unique("prefix")`,
  otherwise two entities created in the same test may collide.
- Related ids come from inserted entities, never from literals.
