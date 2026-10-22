# Discovery and inventory

Goal: an evidence-backed picture of the repository before any change. Every finding cites a file (and line when it
matters). Read code and configuration; do not infer from package names alone.

Search with `git grep` (tracked files only), so `bin/`, `obj/` and other ignored output never produce false hits.
Discovery is read-only: no `dotnet build`, no restore, no database connections (see § 5 for checking migrations
without building or connecting).

## 1. Solution and build

```bash
git ls-files '*.sln' '*.slnx' '*.slnf'
git ls-files '*.csproj' '*.fsproj' '*.vbproj' '*.sqlproj'
cat global.json Directory.Build.props Directory.Build.targets Directory.Packages.props 2>/dev/null
dotnet sln <solution> list
```

- **Authoritative solution.** One root solution is the usual answer. If several exist, prefer the one CI builds
  (check `.github/workflows`, `azure-pipelines*.yml`, `build.*` scripts) and the one referenced by README/CLAUDE.md.
  If `.sln` and `.slnx` both exist with the same projects, ask which is the source of truth, then keep both
  consistent only if the repository already does so. Never migrate one format to the other.
- **Application name `{App}`.** Take the common prefix of the application project names (`Contoso.Orders.Api`,
  `Contoso.Orders.Worker` give `Contoso.Orders`). Otherwise use the solution file name. If several candidates exist,
  ask. Every new project is named `{App}.<Role>` (`apphost.md` § Project naming).
- **Solution folders.** Record the existing folder names (`/src/`, `/tests/`…). All new local projects go in one
  `/local/` folder (`--solution-folder local`), unless the solution already nests folders by path. Then mirror that
  (`--solution-folder local/aspire`). `--solution-folder src` adds to the existing `/src/` folder (verified with
  `.slnx`).
- **SDK and TFMs.** `global.json` (`sdk.version`, `rollForward`), each project's `TargetFramework(s)`. The AppHost
  template supports `net8.0`–`net11.0` in 13.6.x; match the repository's highest application TFM that the installed
  SDK supports.
- **Central package management.** If `Directory.Packages.props` has `ManagePackageVersionsCentrally=true`, new package
  versions go there and `PackageReference` has no `Version`. Note `CentralPackageTransitivePinningEnabled`.
- **Shared props.** `Directory.Build.props` settings (`TreatWarningsAsErrors`, analyzers, `Nullable`, `LangVersion`)
  apply to new projects under the same tree, including `local/`. Check that the AppHost builds under them.

## 2. Executable components

For each project: SDK (`Microsoft.NET.Sdk.Web`, `.Worker`, `Microsoft.NET.Sdk` + `OutputType Exe`), entry point
(`Program.cs`, top-level statements), hosting model, `Properties/launchSettings.json` profiles and ports, project
references. Note which launch profile Aspire will use for each project. The order is: `launchProfileName` in
`AddProject`, then the profile with the same name as the AppHost's profile (`https` by default), then the first
profile. The chosen profile decides the environment (`ASPNETCORE_ENVIRONMENT`) and the URLs.

Classify: HTTP API, web front end, gRPC service, background worker, scheduled job (Quartz, Hangfire, cron hosted
services), Azure Functions, console tools, libraries. Only executables become Aspire project resources. Libraries are
never resources.

## 3. Existing Aspire and local infrastructure

```bash
git ls-files | grep -i -E 'apphost|servicedefaults|aspire\.config\.json|\.aspire/settings\.json'
git ls-files | grep -i -E 'docker-compose|compose\.ya?ml|Dockerfile|\.devcontainer|tilt|skaffold'
git grep -l -E 'Aspire\.AppHost\.Sdk|Aspire\.Hosting|IsAspireSharedProject' -- '*.csproj'
aspire ls --format Json
dotnet new list aspire-apphost        # templates installed? decides the AppHost creation method (apphost.md)
ls -d .claude/skills/*/ .agents/skills/*/ .github/skills/*/ .opencode/skill/*/ 2>/dev/null; ls .mcp.json 2>/dev/null
```

The last line records existing agent skill locations and MCP configuration, which Phase 7 needs.

State:

| State | Signals |
|---|---|
| Missing | No AppHost, no `Aspire.AppHost.Sdk`. |
| Partially configured | AppHost exists but misses executables or infrastructure the code needs; not in the solution; no database initialization; services don't call service defaults. |
| Configured incorrectly | AppHost in `src/`; deployable projects referencing the AppHost or local-only projects; hardcoded connection strings/ports in the AppHost; `WithReference` without `WaitFor` where readiness matters; multiple competing migration runners; Compose and Aspire both owning the same database. |
| Compliant | All rules in SKILL.md hold and validation passes. |

For Docker Compose: list each service, which ones Aspire should own (one owner per piece of infrastructure), and
which Compose files are also used by CI or other environments (those stay).

## 4. Dependencies from code and configuration

Search configuration keys and their consumers together:

```bash
git grep -n -E '"ConnectionStrings"|"Redis"|"RabbitMQ"|"Kafka"|"ServiceBus"|"Storage"|"BaseUrl"|"Endpoint"' -- 'src/*.json'
git grep -n -E 'GetConnectionString\(|GetSection\(|Configure<|AddDbContext|UseSqlServer|UseNpgsql|UseMySql|UseSqlite|UseOracle|UseCosmos|NpgsqlDataSource|SqlConnection|MySqlConnection|IConnectionMultiplexer|ConnectionMultiplexer|AddStackExchangeRedis|IMongoClient|RabbitMQ|IConnectionFactory|MassTransit|NServiceBus|Confluent\.Kafka|ServiceBusClient|BlobServiceClient|AmazonS3|AddHttpClient|HttpClient\b|GrpcChannel|AddGrpcClient' -- 'src/*.cs'
```

For each candidate dependency, confirm it is actually used on a runtime path (registered and resolved, or called),
then record:

| Field | Example |
|---|---|
| Name and kind | `orders` PostgreSQL database |
| Evidence | `src/Orders.Api/Program.cs:14` `GetConnectionString("orders")` |
| Consumers | `Orders.Api`, `Orders.Worker` |
| Configuration key | `ConnectionStrings:orders` |
| Classification | runtime-required / development-only / test-only / external / optional / unused (package referenced, never used: report it, don't provision it, don't remove it unless asked) |
| Local replacement | Aspire PostgreSQL container, emulator, none |

External services (third-party APIs, corporate identity providers, SaaS) stay external unless an official emulator
exists and the code supports pointing at it. Record which credentials or settings developers must supply.

Feature flags and optional dependencies: if the code tolerates the dependency being absent (null checks, feature
toggles), classify as optional and do not provision it by default.

Secrets already committed (for example a developer password in `appsettings.Development.json`) are a gap to report.
Give the file:line and the key, with the value redacted. Do not change or remove them unless the user asks, and
remember that Aspire's environment variables override them at runtime.

## 5. Databases

For every database record: engine and version (from Compose image tags, docs, provider package, migration SQL
dialect), provider package, accessing projects, data access (EF Core, Dapper, ADO.NET, stored procedures), schema
source (EF migrations, `.sql` scripts, DbUp/FluentMigrator/Evolve/Flyway/Liquibase, `.sqlproj`/DACPAC, none), seed
data, and how a fresh database is initialized today. Use `references/database.md` to select the strategy.

```bash
git ls-files | grep -i -E 'Migrations/|\.sql$|dbup|fluentmigrator|evolve|flyway|liquibase|\.sqlproj$|\.dacpac$|seed'
git grep -n -E ': DbContext|IDesignTimeDbContextFactory|Database\.Migrate|MigrateAsync|EnsureCreated|HasData\(' -- 'src/*.cs'
```

During discovery, check migrations from the files: the `Migrations/` folder, the model snapshot, and migration class
names in order. The build-based checks in `database.md` (`dotnet ef migrations list --no-connect`,
`has-pending-model-changes`) need a build. In audit mode run them only with `--no-build` against existing, up-to-date
build output; otherwise mark them "not verified (needs build)". `dotnet ef migrations list` without `--no-connect`
connects to the configured database. Never do that during discovery.

`EnsureCreated` combined with migrations is a defect to report: they are incompatible.

## 6. Telemetry, health and resilience

```bash
git grep -n -E 'AddOpenTelemetry|UseOtlpExporter|AddOtlpExporter|Serilog|NLog|AddApplicationInsights|AddHealthChecks|MapHealthChecks|AddServiceDiscovery|AddStandardResilienceHandler|AddServiceDefaults|MapDefaultEndpoints' -- 'src/*.cs'
```

Note existing logging providers (Serilog sinks, Application Insights) and whether they would conflict with an OTLP
pipeline. Note the health endpoints that already exist and which environments map them.

## 7. Existing tests and docs

Test projects, frameworks, how integration tests get infrastructure today (Testcontainers, Aspire testing, shared
database). Existing local docs/scripts (`README`, `docs/`, `scripts/`, `Makefile`, `*.ps1`/`*.sh`). They must stay
consistent with the new environment, or be updated in the same change.

## Gap report template

```text
Repository: <name>   App name ({App}): <value> (from <prefix|solution name>)   Solution: <path> (<sln|slnx>)
Tooling:       SDK <version> (global.json: <…>)   Aspire CLI <version>   Container runtime <…>   Aspire templates <installed|not installed>
Aspire state:  <missing|partial|incorrect|compliant>   Running AppHosts: <aspire ps result>

Build:         TFMs <…>   CPM <on|off>   Directory.Build.props <relevant settings|none>   Solution folders <…>
Executables:   <project> (<kind>, launch profile Aspire will use, environment) …
Dependencies:  <name> | <kind> | <classification> | <evidence> | <config key> | <local replacement>
Databases:     <name> | <engine/provider/version> | <consumers> | <data access> | <schema source> | <init today> | <persistence> | <strategy A–E + why>
Telemetry:     <logging/tracing/metrics, health endpoints + environments, service discovery, resilience> → <proposed change>
Agent tooling: <existing skill locations, .mcp.json>
Tests/docs:    <test projects + infrastructure, local docs/scripts>
Secrets found: <file:line key = <redacted>> …
Gaps:          1. … (rule violated, evidence)
Plan:          1. … (files, solution folder, commands)
Needs your decision: preview packages, global installs, moving the AppHost, external credentials, …
```
