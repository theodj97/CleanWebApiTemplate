# Discovery: analyse the repository before generating anything

Everything this skill generates depends on facts about the repository: the engine behind each connection string, the
exact configuration keys, how the schema is built, which roles exist, how tokens are validated, what tests already
exist. A wrong guess produces tests that pass against the wrong thing (a developer database, a real API) or fail for
reasons unrelated to the code. So inspect first, report, ask about anything ambiguous, and only then generate.

## Contents
- [0. Prerequisites](#0-prerequisites)
- [1. Deployable applications and entry points](#1-deployable-applications-and-entry-points)
- [2. External dependencies](#2-external-dependencies)
- [3. Database schema strategy and seed data](#3-database-schema-strategy-and-seed-data)
- [4. Configuration names](#4-configuration-names)
- [5. Authentication and authorization](#5-authentication-and-authorization)
- [6. Existing tests](#6-existing-tests)
- [7. Solution format and build conventions](#7-solution-format-and-build-conventions)
- [8. Decisions and questions](#8-decisions-and-questions)
- [Report templates](#report-templates)

## 0. Prerequisites

Run these before anything else and report the result at the top of the report:

```bash
docker info --format "{{.ServerVersion}}"   # or: podman info
dotnet --list-sdks
```

- No container runtime running: say so immediately. Unit tests can still be written and run; component and functional
  tests can be generated and built but not executed until Docker/Podman runs.
- Podman only: Aspire needs `ASPIRE_CONTAINER_RUNTIME=podman` in the environment of the test run; tell the user and
  document it in `tests/README.md`.
- `global.json`: the pinned SDK (all `dotnet` commands use it) and a `"test": { "runner": ... }` section, which decides
  how `dotnet test` runs on the .NET 10+ SDK (see project-setup.md, "Choosing the xUnit packages").

## 1. Deployable applications and entry points

Find every project that is deployed on its own. One trio of test projects (Unit/Component/Functional) is created per
deployable application.

| Signal | Meaning |
|---|---|
| `<Project Sdk="Microsoft.NET.Sdk.Web">` | ASP.NET Core app (API, gRPC service, Blazor server) |
| `<Project Sdk="Microsoft.NET.Sdk.Worker">`, `Host.CreateApplicationBuilder`, only `BackgroundService`s | Worker |
| `<AzureFunctionsVersion>`, `Microsoft.Azure.Functions.Worker` | Azure Functions (isolated) |
| `Aspire.AppHost.Sdk` | Existing Aspire AppHost (development orchestration: read it, do not reuse it for tests) |
| `*.ServiceDefaults` project | Aspire service defaults: production code, never referenced by test infrastructure |
| `OutputType` Library | Not deployable: tested through the apps that use it, or with unit tests |

For each app record: project path, `TargetFramework(s)`, entry point (`Program.cs` with top-level statements,
`WebApplication.CreateBuilder(args)` or `CreateBuilder()` without args — this matters for configuration overrides,
see component-tests.md), legacy `Startup`, and whether an explicit `partial class Program` exists (.NET 10 generates a
public one automatically; .NET 8/9 need `public partial class Program;` for `WebApplicationFactory<Program>`).

Also note environment-dependent code: `if (app.Environment.IsDevelopment())` branches, dev-only seeding or migrations,
Swagger, `appsettings.{Environment}.json` files, and health endpoints (`MapHealthChecks`, ServiceDefaults'
`MapDefaultEndpoints`, which maps `/health` and `/alive` only in Development). The test environment name is a decision
for the report, and the functional AppHost may only use a health check that is mapped in that environment.

## 2. External dependencies

Combine four sources; any single one misses things.

1. **NuGet packages** of each app and the libraries it references.
2. **DI registrations** in `Program.cs` and the `Add*` extension methods it calls (follow them into the libraries).
3. **Configuration**: every `appsettings*.json`, `launchSettings.json`, environment variable usage, `IOptions<T>`
   bindings (`Configure<T>(GetSection(...))`, `BindConfiguration("...")`, `AddOptions<T>().Bind(...)`).
4. **Clients**: typed `HttpClient`s, Refit interfaces, gRPC clients, SDK clients created by hand (`new HttpClient`,
   `new BlobServiceClient(...)`), hard-coded URLs.

| Dependency | Typical signals |
|---|---|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Data.SqlClient`, `UseSqlServer`, `Aspire.Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `UseNpgsql`, `AddNpgsqlDbContext<T>("name")` |
| MySQL / MariaDB | `Pomelo.EntityFrameworkCore.MySql`, `MySqlConnector`, `MySql.EntityFrameworkCore`, `UseMySql` |
| Oracle | `Oracle.EntityFrameworkCore`, `Oracle.ManagedDataAccess.Core`, `UseOracle` |
| MongoDB | `MongoDB.Driver`, `MongoDB.EntityFrameworkCore`, `IMongoDatabase` |
| Redis | `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `AddStackExchangeRedisCache`, `IConnectionMultiplexer` |
| Kafka | `Confluent.Kafka`, `ProducerBuilder`, `ConsumerBuilder`, topic names in config/constants, consumer `GroupId` |
| RabbitMQ | `RabbitMQ.Client`, `ConnectionFactory`, exchange/queue declarations |
| Azure Service Bus | `Azure.Messaging.ServiceBus`, `ServiceBusClient`, queue/topic/subscription names |
| Messaging abstraction | `MassTransit.*`, `WolverineFx.*`, `DotNetCore.CAP.*`, `NServiceBus.*`, `Rebus.*` (record which transport it uses) |
| Azure Storage / Cosmos DB | `Azure.Storage.Blobs/Queues`, `Azure.Data.Tables`, `Microsoft.Azure.Cosmos` |
| AWS | `AWSSDK.S3`, `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `AWSSDK.DynamoDBv2` |
| SMTP | `MailKit`, `System.Net.Mail.SmtpClient`, `FluentEmail.*`, SMTP host/port settings |
| Third-party HTTP APIs | `AddHttpClient<T>`, `AddRefitClient<T>`, `BaseAddress`, URLs in configuration |
| gRPC services | `Grpc.Net.Client`, `AddGrpcClient<T>`, `.proto` files with client stubs |
| Identity provider | `AddJwtBearer`, `Microsoft.Identity.Web`, `AddOpenIdConnect`, authority URLs |
| Secrets / config services | `AddAzureKeyVault`, `AddAzureAppConfiguration`, AWS Parameter Store providers |
| Aspire service discovery | `AddServiceDiscovery`, URLs like `https+http://catalog` (configured through `services:catalog:http:0`) |

Aspire client integrations (`builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb")`, `AddRedisClient("cache")`,
`AddKafkaProducer<,>("kafka")`...) read `ConnectionStrings:{name}`: the name in the call is the configuration key.

Record per dependency: kind, engine/product and version if pinned, logical name (database name, topics, queues,
exchanges, consumer group ids), the configuration key that points to it, and the abstraction library if any.
Configuration providers that call a cloud service at startup (Key Vault, App Configuration) are dependencies too:
find out whether they are conditional (environment, a config flag); if not, it is a question for the user.

## 3. Database schema strategy and seed data

| Signal | Strategy |
|---|---|
| `Migrations/` folder with `*ModelSnapshot.cs`, `[Migration]` classes | EF Core migrations |
| `Database.Migrate()`/`MigrateAsync()` in Program.cs or a hosted service | App migrates on startup |
| `Database.EnsureCreated()` | EF without migrations (or app creates it) |
| `DbUp` packages, `DeployChanges.To...` | DbUp (record script location: embedded resources or folder) |
| `FluentMigrator*`, `[Migration(20240101...)]` from FluentMigrator | FluentMigrator |
| `.sqlproj` (`Microsoft.Build.Sql` SDK or SSDT), `.dacpac` | Database project / DACPAC |
| Loose `.sql` files (`schema.sql`, `db/`, `scripts/`) applied by a pipeline | Scripts |
| None of the above | No mechanism: a schema script must be inferred (ask, see database.md) |

Also record:
- EF specifics that tests must replicate: custom history table (`MigrationsHistoryTable("...", "schema")`), default
  schema (`HasDefaultSchema`), migrations assembly, naming conventions (`UseSnakeCaseNamingConvention`), provider
  options (`UseNodaTime`, `EnableRetryOnFailure`), interceptors.
- Seed data: `HasData(...)`, EF 9+ `UseSeeding/UseAsyncSeeding`, seed scripts, startup seeders. Seeded reference
  tables are candidates for `TablesToIgnore`; tables that tests also write to are not (their rows are re-seeded).
- Tables that must never be emptied: migration history (`__EFMigrationsHistory`, DbUp `SchemaVersions`,
  FluentMigrator `VersionInfo`), reference data only read by the app.
- Non-relational engines, which Respawn does not cover: for MongoDB record the collection names the app reads and writes
  (entity mappings, repositories, `GetCollection<T>("...")`); for Redis record the logical database and the key-prefix
  convention (key builders, cache instance names, `IConnectionMultiplexer` usages). They become the allowlists
  `MongoCollectionsToReset` / `RedisKeyPrefixesToReset` in `DatabaseResetConfiguration`.

## 4. Configuration names

The AppHost and the component fixture must inject configuration under the exact names the app reads, so list them
literally, e.g. `ConnectionStrings:OrdersDb`, `Kafka:BootstrapServers`, `PaymentsApi:BaseUrl`,
`Authentication:Schemes:Bearer:Authority`. Record:
- connection string names (`GetConnectionString("X")`, Aspire client integration names);
- every option section and key that points to infrastructure or to a third party;
- **every URL of a real service** found anywhere (appsettings of every environment, code, launch profiles). Each one
  must be overridden in tests: nothing may leave the machine. A hard-coded URL that cannot be overridden through
  configuration is a question for the user (changing it is a production change).

## 5. Authentication and authorization

- **Scheme**: `AddJwtBearer` (scheme names, possibly several), `AddMicrosoftIdentityWebApi` (Microsoft.Identity.Web),
  cookies (`AddCookie`), API keys (custom handler), OpenID Connect, none.
- **Token validation**: `Authority`/`MetadataAddress`, `Audience`/`ValidAudience(s)`, `ValidIssuer(s)`,
  `RequireHttpsMetadata`, signing key from configuration (symmetric `IssuerSigningKey`). Note whether options are bound
  from configuration (`Authentication:Schemes:{Scheme}:*` is bound automatically since .NET 7; `AzureAd:*` for
  Microsoft.Identity.Web) or set in code — only configuration can be changed for functional tests without touching
  production code.
- **Roles and permissions**: `[Authorize(Roles = ...)]`, `[Authorize(Policy = ...)]`, `[AllowAnonymous]`,
  `AddAuthorization`/`AddAuthorizationBuilder().AddPolicy(...)` with `RequireRole`, `RequireClaim`, `RequireAssertion`,
  `RequireAuthorization(...)` on minimal API endpoints and route groups, `FallbackPolicy`/`DefaultPolicy` (an
  authenticated-by-default app), role constant classes, custom `IAuthorizationHandler`/requirements, Microsoft.Identity.Web
  `[RequiredScope]`/`RequireScope`.
- **Claims**: role claim type (`role`, `roles`, `ClaimTypes.Role`, `TokenValidationParameters.RoleClaimType`),
  `MapInboundClaims` (on by default: `role` and `roles` both become `ClaimTypes.Role`), other claims the app reads
  (`sub`/`oid` as user id, `scp` scopes, `tid`/tenant, custom claims), `IClaimsTransformation` that adds roles from
  the database.
- **Users in the database**: a users table linked by `sub`/`oid`, middleware or services that load the current user
  and fail when it does not exist. If so, test users must exist in the database after every reset.

## 6. Existing tests

Record: test projects and their framework (xUnit v2/v3 and exact package flavour, NUnit, MSTest), runner mode
(VSTest/MTP), mocking (Moq, NSubstitute, FakeItEasy), assertions (xUnit Assert, FluentAssertions, Shouldly,
AwesomeAssertions), data (Bogus, AutoFixture), infrastructure (Testcontainers, existing `WebApplicationFactory`
subclasses, fixtures, base classes, docker-compose for tests), naming convention (sample five test names), folder
layout, and whether they pass today (`dotnet test` on them, if cheap). Existing conventions win over this skill's
defaults; never upgrade or migrate them without being asked.

## 7. Solution format and build conventions

- `.sln` or `.slnx` (and the SDK supports `dotnet sln` on `.slnx` from 9.0.200).
- `Directory.Packages.props` (`ManagePackageVersionsCentrally`, ordering, grouping, `GlobalPackageReference`,
  conditions per TFM), `Directory.Build.props/targets` (TreatWarningsAsErrors, Nullable, LangVersion, analyzers,
  `IsPackable`) — new test projects inherit them, so generated code must compile cleanly under them.
- `NuGet.config` with private feeds (package resolution must use them).

## 8. Decisions and questions

List the decisions you propose (test environment name, schema mechanism, auth strategy for functional tests, which
background services to disable, xUnit package flavour) and the questions that block generation. Ask before generating
when, for example: the schema cannot be rebuilt; roles are unclear or computed at runtime; a URL is hard-coded; the
functional auth strategy needs a production change (`RequireHttpsMetadata` fixed in code); a dependency has no good
substitute; existing tests use another framework or Testcontainers.

## Report templates

### Setup mode (full report — wait for the user's confirmation before generating)

```markdown
## Test infrastructure analysis — {Repository}

**Prerequisites**: Docker {version|NOT RUNNING} · .NET SDKs {list} · global.json {sdk, test runner}

1. **Deployable applications**: {App} ({path}, net{X}, Web API, entry point Program.cs with CreateBuilder(args)) …
   → test projects: {App}.UnitTests / .ComponentTests / .FunctionalTests
2. **External dependencies**:
   | Dependency | Product | Name(s) | Config key | Test substitute |
3. **Database schema**: {strategy}; seed data {…}; tables kept on reset: {…}; MongoDB collections / Redis key prefixes
   reset: {…}
4. **Configuration keys to inject**: {key → resource}
5. **Authentication/authorization**: scheme {…}; validation {authority, issuer, audience, config-bound?};
   roles/policies {…}; role claim {…}; other claims {…}; users in DB {yes/no}
   → TestUser values: {Admin, Customer, …, AuthenticatedWithoutRoles}
6. **Existing tests**: {none | projects, xUnit version, libraries, conventions}
7. **Solution**: {file}.slnx/.sln; CPM {yes/no}; Directory.Build.props {relevant settings}
8. **Proposed decisions**: {…}
   **Questions**: {…}
```

### Incremental mode (short report — proceed unless something is blocking)

```markdown
**Mode**: incremental · **Target**: {endpoint/flow/class} · **Test type(s)**: {…}
**Reusing**: {fixture, base class, repositories, builders, helpers}
**Adding**: {test class, builder/repository method, TestUser value, stub helper — additive only}
**Behaviour found**: {contract, validation, authorization, side effects, third parties}
**Open questions**: {none | …}
```
