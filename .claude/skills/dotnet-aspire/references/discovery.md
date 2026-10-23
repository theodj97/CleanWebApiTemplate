# Discovery and inventory

Canonical for SKILL.md rule 5 (evidence over assumption) and Phases 0–2. Everything this skill generates depends on
facts about the repository: the engine behind each connection string, the exact configuration keys, how the schema is
built, which roles and claims exist, how tokens are validated, what tests and AppHosts already exist. A wrong guess
produces an environment or tests that work against the wrong thing (a developer database, a real API, a real identity
provider). So inspect first, report, ask about anything ambiguous, and only then change.

Every finding cites a file (and line when it matters). Read code and configuration; never infer from package names
alone. Search with `git grep` (tracked files only), so `bin/`, `obj/` and other ignored output never produce false
hits. **Discovery is read-only in every mode**: no builds, restores, database connections or test runs. Running the
existing tests as a baseline happens after the gap report (Phase 2, "safe independent work"), never in `audit`.

## Contents
- [0. Tooling and workspace state](#0-tooling-and-workspace-state)
- [1. Solution and build](#1-solution-and-build)
- [2. Executable components](#2-executable-components)
- [3. Existing Aspire, local and test infrastructure](#3-existing-aspire-local-and-test-infrastructure)
- [4. Dependencies and configuration names](#4-dependencies-and-configuration-names)
- [5. Databases](#5-databases)
- [6. Authentication and authorization](#6-authentication-and-authorization)
- [7. Telemetry, health and resilience](#7-telemetry-health-and-resilience)
- [8. Existing tests and docs](#8-existing-tests-and-docs)
- [9. Decisions and questions](#9-decisions-and-questions)
- [Report templates](#report-templates)

## 0. Tooling and workspace state

```bash
git status --short
dotnet --info                                # SDKs, runtimes; also: cat global.json (sdk.version, rollForward, "test": { "runner" })
aspire --version
docker info --format '{{.ServerVersion}}'    # or: podman info
aspire ps --format Json                      # AppHosts already running: never stop or duplicate them
dotnet new list aspire-apphost               # Aspire templates installed? (decides how the AppHost is created)
dotnet dev-certs https --check --trust       # HTTPS development certificate trusted? (trusted by default in setup/repair)
```

- Read `CLAUDE.md`, `AGENTS.md` and equivalent instruction files, and honor them. Note uncommitted user changes and do
  not overwrite them.
- Aspire CLI missing: continue with discovery and file work, and report the official installation instructions
  (`sources.md`).
- No container runtime: say so immediately. Unit tests can be written and run; files, component and functional tests
  can be created and built but not run, and the environment cannot be started. On Podman, Aspire needs
  `ASPIRE_CONTAINER_RUNTIME=podman` (document it in `local/local-development.md` and `tests/README.md`).
- `global.json` `"test": { "runner": … }` decides how `dotnet test` runs on the .NET 10+ SDK
  (`architecture-and-layout.md` § Choosing the xUnit packages). The SDK also decides whether `devtoken.cs` can be a
  file-based app (.NET 10+).

## 1. Solution and build

```bash
git ls-files '*.sln' '*.slnx' '*.slnf'
git ls-files '*.csproj' '*.fsproj' '*.vbproj' '*.sqlproj'
cat global.json Directory.Build.props Directory.Build.targets Directory.Packages.props NuGet.config 2>/dev/null
dotnet sln <solution> list
```

- **Authoritative solution.** Usually one root solution. If several exist, prefer the one CI builds
  (`.github/workflows`, `azure-pipelines*.yml`, `build.*` scripts) and the one README/CLAUDE.md references. If `.sln`
  and `.slnx` both exist with the same projects, ask which is the source of truth; keep both consistent only if the
  repository already does. Never migrate one format to the other. A second solution under `local/` is a gap to report.
- **Application name `{App}`**: `architecture-and-layout.md` § Project naming.
- **Solution folders.** Record existing names (`/src/`, `/tests/`…) and whether folders are nested by path.
- **SDK and TFMs.** `global.json`, each project's `TargetFramework(s)`; the SDK supports `dotnet sln` on `.slnx` from
  9.0.200.
- **CPM and shared props.** `ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled`,
  `GlobalPackageReference`, conditions per TFM; `Directory.Build.props` settings (`TreatWarningsAsErrors`, analyzers,
  `Nullable`, `LangVersion`, `IsPackable`) that new projects inherit, including `local/` and `tests/`.
- **Private feeds** in `NuGet.config`: package resolution must use them.

## 2. Executable components

For each project: SDK (`Microsoft.NET.Sdk.Web`, `.Worker`, `Microsoft.NET.Sdk` + `OutputType Exe`), entry point
(`Program.cs` with top-level statements, `WebApplication.CreateBuilder(args)` or `CreateBuilder()` without args — it
matters for configuration overrides in component tests), legacy `Startup`, explicit `partial class Program` (.NET 10
generates a public one; .NET 8/9 need `public partial class Program;` for `WebApplicationFactory<Program>`),
`Properties/launchSettings.json` profiles and ports, project references.

| Signal | Meaning |
|---|---|
| `Microsoft.NET.Sdk.Web` | ASP.NET Core app (API, gRPC service, Blazor server) |
| `Microsoft.NET.Sdk.Worker`, `Host.CreateApplicationBuilder`, only `BackgroundService`s | Worker |
| `<AzureFunctionsVersion>`, `Microsoft.Azure.Functions.Worker` | Azure Functions (isolated) |
| `Aspire.AppHost.Sdk` | Existing AppHost (section 3) |
| `*.ServiceDefaults` | Aspire service defaults: production code, never referenced by test projects |
| `OutputType` Library | Not deployable: tested through the apps that use it, or with unit tests |

Classify: HTTP API, web front end, gRPC service, background worker, scheduled job (Quartz, Hangfire, cron hosted
services), Azure Functions, console tools, libraries. Only executables become Aspire project resources. One test trio
per deployable application.

Record which launch profile Aspire will use in Local mode (`launchProfileName` in `AddProject`, then the profile named
like the AppHost's, `https` by default, then the first profile) and the environment it gives. Note
environment-dependent code: `if (app.Environment.IsDevelopment())` branches, dev-only seeding or migrations, Swagger,
`appsettings.{Environment}.json`, health endpoints (`MapHealthChecks`, `MapDefaultEndpoints`, which maps `/health` and
`/alive` only in Development). The test environment name is a decision for the report.

## 3. Existing Aspire, local and test infrastructure

```bash
git ls-files | grep -i -E 'apphost|servicedefaults|aspire\.config\.json|\.aspire/settings\.json'
git ls-files | grep -i -E 'docker-compose|compose\.ya?ml|Dockerfile|\.devcontainer|tilt|skaffold'
git grep -l -E 'Aspire\.AppHost\.Sdk|Aspire\.Hosting|IsAspireSharedProject' -- '*.csproj'
git grep -n -E 'DistributedApplicationTestingBuilder|TestHostContract|TestMode' -- '*.cs'
aspire ls --format Json
ls -d .claude/skills/*/ .agents/skills/*/ .github/skills/*/ .opencode/skill/*/ 2>/dev/null; ls .mcp.json 2>/dev/null
```

State of the environment:

| State | Signals |
|---|---|
| Missing | No AppHost, no `Aspire.AppHost.Sdk`. |
| Partially configured | AppHost exists but misses executables or infrastructure the code needs; not in the solution; no database initialization; services don't call service defaults; no mode switch; no local JWT trust. |
| Configured incorrectly | AppHost in `src/`; **a second AppHost for tests** (e.g. `tests/orchestration/{App}.Tests.AppHost`, `tests/{App}.TestAppHost`); deployable projects referencing the AppHost or local-only projects; hardcoded connection strings/ports; `WithReference` without `WaitFor` where readiness matters; `WithDataVolume`/bind mounts/persistent lifetimes on data resources; no seed step; competing migration runners; Compose and Aspire both owning the same database; an `Authority`/identity provider reachable from Local or test modes. |
| Compliant | All SKILL.md rules hold and validation passes. |

For Docker Compose: list each service, which ones Aspire should own (one owner per piece of infrastructure), and which
Compose files CI or other environments also use (those stay). For an existing test AppHost: record its resources,
injected keys, third-party substitutes, identity stubs and mode switch: they feed the consolidation plan
(`apphost.md` § Consolidating a dedicated test AppHost).

## 4. Dependencies and configuration names

Combine four sources; any single one misses things: NuGet packages of each app and the libraries it references; DI
registrations in `Program.cs` and the `Add*` extensions it calls (follow them into the libraries); configuration
(every `appsettings*.json`, `launchSettings.json`, environment variable usage, `IOptions<T>` bindings); clients
(typed `HttpClient`s, Refit, gRPC clients, SDK clients created by hand, hard-coded URLs).

```bash
git grep -n -E '"ConnectionStrings"|"Redis"|"RabbitMQ"|"Kafka"|"ServiceBus"|"Storage"|"BaseUrl"|"Endpoint"|"Authority"' -- 'src/*.json'
git grep -n -E 'GetConnectionString\(|GetSection\(|Configure<|BindConfiguration|AddDbContext|UseSqlServer|UseNpgsql|UseMySql|UseSqlite|UseOracle|UseCosmos|NpgsqlDataSource|SqlConnection|MySqlConnection|IConnectionMultiplexer|AddStackExchangeRedis|IMongoClient|RabbitMQ|IConnectionFactory|MassTransit|NServiceBus|Confluent\.Kafka|ServiceBusClient|BlobServiceClient|AmazonS3|AddHttpClient|HttpClient\b|GrpcChannel|AddGrpcClient|AddAzureKeyVault|AddAzureAppConfiguration' -- 'src/*.cs'
```

| Dependency | Typical signals |
|---|---|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Data.SqlClient`, `UseSqlServer`, `Aspire.Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `UseNpgsql`, `AddNpgsqlDbContext<T>("name")` |
| MySQL / MariaDB | `Pomelo.EntityFrameworkCore.MySql`, `MySqlConnector`, `MySql.EntityFrameworkCore`, `UseMySql` |
| Oracle | `Oracle.EntityFrameworkCore`, `Oracle.ManagedDataAccess.Core`, `UseOracle` |
| MongoDB | `MongoDB.Driver`, `MongoDB.EntityFrameworkCore`, `IMongoDatabase` |
| Redis | `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `AddStackExchangeRedisCache`, `IConnectionMultiplexer` |
| Kafka | `Confluent.Kafka`, `ProducerBuilder`, `ConsumerBuilder`, topic names, consumer `GroupId` |
| RabbitMQ | `RabbitMQ.Client`, `ConnectionFactory`, exchange/queue declarations |
| Azure Service Bus | `Azure.Messaging.ServiceBus`, `ServiceBusClient`, queue/topic/subscription names |
| Messaging abstraction | `MassTransit.*`, `WolverineFx.*`, `DotNetCore.CAP.*`, `NServiceBus.*`, `Rebus.*` (record the transport) |
| Azure Storage / Cosmos DB | `Azure.Storage.Blobs/Queues`, `Azure.Data.Tables`, `Microsoft.Azure.Cosmos` |
| AWS | `AWSSDK.S3`, `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `AWSSDK.DynamoDBv2` |
| SMTP | `MailKit`, `System.Net.Mail.SmtpClient`, `FluentEmail.*`, SMTP host/port settings |
| Third-party HTTP APIs | `AddHttpClient<T>`, `AddRefitClient<T>`, `BaseAddress`, URLs in configuration |
| gRPC services | `Grpc.Net.Client`, `AddGrpcClient<T>`, `.proto` files with client stubs |
| Identity provider | `AddJwtBearer`, `Microsoft.Identity.Web`, `AddOpenIdConnect`, authority URLs (section 6) |
| Secrets / config services | `AddAzureKeyVault`, `AddAzureAppConfiguration`, AWS Parameter Store providers |
| Aspire service discovery | `AddServiceDiscovery`, URLs like `https+http://catalog` (`services:catalog:http:0`) |

Aspire client integrations (`builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb")`, `AddRedisClient("cache")`…)
read `ConnectionStrings:{name}`: the name in the call is the configuration key.

For each candidate dependency, confirm it is used on a runtime path (registered and resolved, or called), then record:

| Field | Example |
|---|---|
| Name and kind | `orders` PostgreSQL database |
| Evidence | `src/Orders.Api/Program.cs:14` `GetConnectionString("orders")` |
| Consumers | `Orders.Api`, `Orders.Worker` |
| Configuration key | `ConnectionStrings:orders` (list keys literally: `Kafka:BootstrapServers`, `PaymentsApi:BaseUrl`, `Jwt:Key`…) |
| Logical names | database, topics, queues, exchanges, consumer group ids, Redis prefixes |
| Classification | runtime-required / development-only / test-only / external (not reasonably replaceable locally) / optional / unused (package referenced, never used: report it, don't provision it, don't remove it unless asked) |
| Local and test substitute | Aspire PostgreSQL container, emulator, WireMock (test modes), none |

- External services (third-party APIs, corporate services, SaaS) stay external in Local mode unless an official
  emulator exists and the code supports pointing at it; record which credentials developers must supply. Identity
  providers are never a runtime dependency in any mode (section 6).
- Feature flags and optional dependencies: if the code tolerates the dependency being absent, classify as optional
  and do not provision it by default.
- **Every URL of a real service** (appsettings of every environment, code defaults, launch profiles) must be
  overridden in the test modes; a hard-coded URL that configuration cannot override is a question (changing it is a
  production change).
- Configuration providers that call a cloud service at startup (Key Vault, App Configuration) are dependencies: find
  out whether they are conditional (environment, flag); if not, it is a question.
- Secrets already committed (a password in `appsettings.Development.json`, a signing key in a base `appsettings.json`
  that every environment loads) are gaps to report with file:line and key, value redacted. Do not change or remove
  them unless the user asks; Aspire's environment variables override them at runtime.

## 5. Databases

For every database record: engine and version (Compose image tags, docs, provider package, migration SQL dialect),
provider package, accessing projects, data access (EF Core, Dapper, ADO.NET, stored procedures), schema source (EF
migrations, `.sql` scripts, DbUp/FluentMigrator/Evolve/Flyway/Liquibase, `.sqlproj`/DACPAC, none), existing seed data
and how a fresh database is initialized today, and any persistence in the current setup (volumes or persistent
lifetimes are a gap). Use `database.md` to select the strategy.

For the seed, inventory every endpoint that reads or writes each database (route, method, policy, entities, filters,
ownership, state preconditions, foreign keys, reference data): it is the input of `database.md` § Designing the seed
from the endpoints. Recording it is read-only; designing and writing the scripts happens after the plan.

```bash
git ls-files | grep -i -E 'Migrations/|\.sql$|dbup|fluentmigrator|evolve|flyway|liquibase|\.sqlproj$|\.dacpac$|seed'
git grep -n -E ': DbContext|IDesignTimeDbContextFactory|Database\.Migrate|MigrateAsync|EnsureCreated|HasData\(|UseSeeding|UseAsyncSeeding' -- 'src/*.cs'
```

| Signal | Strategy evidence |
|---|---|
| `Migrations/` with `*ModelSnapshot.cs`, `[Migration]` classes | EF Core migrations |
| `Database.Migrate()`/`MigrateAsync()` in `Program.cs` or a hosted service | the app migrates on startup |
| `Database.EnsureCreated()` | EF without migrations (a defect next to migrations) |
| `DbUp` packages, `DeployChanges.To…` | DbUp (script location: embedded resources or folder) |
| `FluentMigrator*` | FluentMigrator |
| `.sqlproj` (`Microsoft.Build.Sql` or SSDT), `.dacpac` | database project / DACPAC |
| Loose `.sql` files applied by a pipeline | scripts |
| None | no mechanism (strategy E) |

Also record what tests must replicate and what resets must keep: custom history table
(`MigrationsHistoryTable("…", "schema")`), default schema, migrations assembly, naming conventions
(`UseSnakeCaseNamingConvention`), provider options (`UseNodaTime`, `EnableRetryOnFailure`), interceptors; seeded
reference tables (candidates for `TablesToIgnore`) versus tables tests also write to; tables that must never be emptied
(migration history, read-only reference data); for MongoDB the collections the app reads and writes, for Redis the
logical database and key-prefix convention (they become the reset allowlists).

During discovery, check migrations from the files (folder, snapshot, class names in order). The build-based checks
(`dotnet ef migrations list --no-connect`, `has-pending-model-changes`) need a build: in audit mode run them only with
`--no-build` against up-to-date output, otherwise mark them "not verified (needs build)". `dotnet ef migrations list`
without `--no-connect` connects to the configured database: never do that during discovery.

## 6. Authentication and authorization

For **every** service that validates tokens (HTTP APIs, gRPC hosts, SignalR hubs, workers), regardless of whether it
also has login endpoints:

- **Schemes**: `AddJwtBearer` (scheme names, possibly several, e.g. an MFA-pending scheme), `AddMicrosoftIdentityWebApi`,
  `AddOpenIdConnect`, cookies (`AddCookie`), API keys (custom handler), custom `AuthenticationHandler<T>`, none.
- **Trust lever** (decides the strategy in `local-jwt-authentication.md`):
  - where the signing key comes from: configuration key and format (`Encoding.UTF8.GetBytes(config["Jwt:Key"])` text,
    base64, PEM, certificate), code, Key Vault;
  - whether options are bound from configuration (`Authentication:Schemes:{Scheme}:*` is bound automatically since .NET
    7; `AzureAd:*` for Microsoft.Identity.Web) or set in code (`options.TokenValidationParameters = new …`);
  - `Authority`/`MetadataAddress` and where they come from; `RequireHttpsMetadata`; `ValidIssuer(s)`,
    `ValidAudience(s)` and whether they are configurable; `ValidAlgorithms`; custom `IssuerValidator`,
    `IssuerSigningKeyResolver`, `SignatureValidator`.
  Only configuration can be changed without touching production code.
- **Token issuers inside the repository** (`JwtService`, login endpoints, refresh tokens): the claim names, issuer,
  audience, lifetime and algorithm they produce are the best evidence of what the validators expect. Record them; do
  not use them to obtain tokens.
- **Roles and permissions**: `[Authorize(Roles = …)]`, `[Authorize(Policy = …)]`, `[AllowAnonymous]`,
  `AddAuthorization`/`AddAuthorizationBuilder().AddPolicy(...)` with `RequireRole`, `RequireClaim`,
  `RequireAssertion`, `RequireAuthorization(...)` on minimal APIs and groups, `FallbackPolicy`/`DefaultPolicy`
  (authenticated by default), role constant classes, custom `IAuthorizationHandler`/requirements,
  `[RequiredScope]`/`RequireScope`.
- **Claims**: role claim type (`role`, `roles`, `ClaimTypes.Role`, `RoleClaimType`), `MapInboundClaims`, other claims
  read (`sub`/`oid` as user id, `scp`/`scope`, `tid`/tenant, custom), `IClaimsTransformation` that adds roles from the
  database. Claim names can differ between services of the same repository.
- **Users in the database**: a users table linked by `sub`/`oid`, middleware that loads the current user and fails when
  it does not exist: test users must exist after every reset, and devtoken subjects may match seeded users.
- **Findings against rule 9**: development-only authentication bypasses, signing keys committed to configuration that
  every environment loads, identity provider calls reachable from local runs or tests, tests that obtain tokens from
  login endpoints or disable validation.

## 7. Telemetry, health and resilience

```bash
git grep -n -E 'AddOpenTelemetry|UseOtlpExporter|AddOtlpExporter|Serilog|NLog|AddApplicationInsights|AddHealthChecks|MapHealthChecks|AddServiceDiscovery|AddStandardResilienceHandler|AddServiceDefaults|MapDefaultEndpoints' -- 'src/*.cs'
```

Existing logging providers (Serilog sinks, Application Insights) and whether they would conflict with an OTLP pipeline;
existing health endpoints and the environments that map them; existing Polly policies (stacking with the standard
resilience handler).

## 8. Existing tests and docs

**API clients.** A brunoGen layer (`git ls-files '*bruno.config.json' '*/custom/manifest.json'`) means the Bruno
collection must be aligned with the local seed (`bruno-collection.md`): record its environments, the collection auth
script (an auto-login against a login endpoint is a rule-9 finding for the local environment), the manifest entries
with literal ids and which environment they belong to, and whether `brunoGen` is installed (`brunoGen --version`).

Test projects and framework (xUnit v2/v3 and exact package flavour, NUnit, MSTest), runner mode (VSTest/MTP), mocking
(Moq, NSubstitute, FakeItEasy), assertions (xUnit Assert, FluentAssertions, Shouldly, AwesomeAssertions), data (Bogus,
AutoFixture), infrastructure (Testcontainers, `WebApplicationFactory` subclasses, fixtures, base classes,
docker-compose for tests, an existing test AppHost), how they authenticate (test scheme, real provider, local tokens),
naming convention (sample five test names), folder layout. Existing conventions win over this skill's defaults; never
upgrade or migrate them without being asked. Whether they pass today is checked after the gap report, not during
discovery. Existing local docs/scripts (`README`, `docs/`, `scripts/`, `Makefile`, `*.ps1`/`*.sh`) must stay consistent
with the new environment, or be updated in the same change.

## 9. Decisions and questions

List the proposed decisions (`{App}` and its source, AppHost location and consolidation, database strategy per
database, trust strategy per protected service and its algorithm, test environment name, schema mechanism in tests,
background services to disable, xUnit flavour, ServiceDefaults) and the questions that block work. Ask before
generating when, for example: the schema cannot be rebuilt; roles are unclear or computed at runtime; a URL is
hard-coded; a trust strategy needs a production change (strategies C/D); a dependency has no good substitute; existing
tests use another framework or Testcontainers; a preview package, a global install, moving or renaming an AppHost,
removing a test AppHost, or external credentials are needed.

## Report templates

### Gap report (audit, setup, repair: wait for confirmation before substantial changes)

```text
Repository: <name>   App ({App}): <value> (from <prefix|solution name>)   Solution: <path> (<sln|slnx>)
Tooling:     SDK <version> (global.json: <…>, test runner <VSTest|MTP>)   Aspire CLI <version>   Container runtime <…>   Aspire templates <yes|no>   Dev certificate <trusted|untrusted|missing>
Aspire:      <missing|partial|incorrect|compliant>   AppHosts: <paths, incl. test AppHosts>   Running: <aspire ps result>
Build:       TFMs <…>   CPM <on|off>   Directory.Build.props <relevant settings|none>   Solution folders <…>
Executables: <project> (<kind>, launch profile, environment, entry point) …
Dependencies: <name> | <kind> | <classification> | <evidence> | <config key> | <local substitute> | <test substitute>
Databases:   <name> | <engine/provider/version> | <consumers> | <data access> | <schema source> | <init today> | <seed today> | <persistence today → ephemeral> | <strategy A–E + why> | <seed plan: endpoints covered, profiles, data per endpoint>
Auth:        <service> | <scheme(s)> | <key source + format> | <config-bound?> | <Authority source> | <issuer/audience configurable?> | <roles/scopes/policies> | <role/scope claim types> | <users in DB?> | <login endpoints> | <trust strategy A–D + algorithm>
Config keys: <key the app reads> → <resource or substitute> (for the AppHost, the component fixture and AppHostContract)
Test reset:  tables kept on reset <…>; MongoDB collections / Redis key prefixes reset <…>; data that must survive <…>
TestUsers:   <TestUser values: one per role, per role/claim combination a policy needs, AuthenticatedWithoutRoles>
Telemetry:   <logging/tracing/metrics, health endpoints + environments, service discovery, resilience> → <proposed change>
Tests:       <projects, framework, flavour, infrastructure, auth in tests, conventions>   Agent tooling: <skill locations, .mcp.json>
Secrets found: <file:line key = <redacted>> …
Gaps:        1. … (rule violated, evidence)
Plan:        1. … (files, solution folder, commands)
Needs your decision: …
```

### Incremental test request (short report: proceed unless something blocks)

```markdown
**Mode**: incremental · **Target**: {endpoint/flow/class} · **Test type(s)**: {…}
**Reusing**: {fixture, base class, repositories, builders, helpers, TestUser values}
**Adding**: {test class, builder/repository method, TestUser value, stub helper, contract key — additive only}
**Behaviour found**: {contract, validation, authorization (policies, roles, scopes), side effects, third parties}
**Open questions**: {none | …}
```
