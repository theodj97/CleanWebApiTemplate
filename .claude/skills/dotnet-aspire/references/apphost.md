# The AppHost: one topology, three modes

Canonical for SKILL.md rule 4. One AppHost at `local/aspire/{App}.AppHost/` is the single source of truth for the
topology of the applications and their dependencies. Local development (`aspire run`) and every test project
(`DistributedApplicationTestingBuilder`) start this same AppHost. Templates: `assets/local/apphost/AppHost.cs` and
`assets/local/apphost/AppHostContract.cs`.

*Changed (new rule):* the former testing skill required a dedicated test-only AppHost. That rule is replaced: its
reasons (data volumes, persistent containers, dashboard, telemetry, cloud resources in the development AppHost) are
now handled by the ephemeral rule (SKILL.md rule 7) and by the test modes below, so a second AppHost would only
duplicate the topology.

## Contents
- [Modes](#modes)
- [The contract](#the-contract)
- [Creating the AppHost](#creating-the-apphost)
- [Hosting integrations](#hosting-integrations)
- [Modelling resources](#modelling-resources)
- [Configuration and secrets](#configuration-and-secrets)
- [Repairing an existing AppHost](#repairing-an-existing-apphost)
- [Consolidating a dedicated test AppHost](#consolidating-a-dedicated-test-apphost)

## Modes

The mode is the configuration key `RunMode` (`AppHostContract.ModeKey`). `aspire run`/`aspire start` pass nothing, so
the default is `Local`; the test fixtures pass `--RunMode=Component|Functional`. Every resource is declared once; a
mode changes configuration, never the shared topology. Unknown values fail fast.

| | Local | Component | Functional |
|---|---|---|---|
| Started by | `aspire run` / `aspire start`, IDE | component test fixture | functional test fixture |
| Infrastructure (DB, cache, broker) | yes, ephemeral | yes, ephemeral | yes, ephemeral |
| DbInitializer | schema **+ seed** (`Seed__Path`) | schema only | schema only |
| Applications | launch profile (Development), `WithHttpHealthCheck` | not declared: the app runs in-process (`WebApplicationFactory`) | `launchProfileName: null`, `WithHttpEndpoint()` (dynamic port), explicit environment (e.g. `Testing`), `WithExplicitStart()` |
| Dashboard / telemetry | dashboard, OTLP, `aspire otel` | dashboard disabled by the fixture; DbInitializer with `OTEL_EXPORTER_OTLP_ENDPOINT` empty | dashboard disabled; `OTEL_EXPORTER_OTLP_ENDPOINT` empty for every project |
| Third-party HTTP APIs | real (parameters) or official emulator | WireMock resource | WireMock resource |
| Outbound safety net | none (developers may need real external services) | `BlockExternalHttpHandler` in the factory | `HTTP_PROXY`/`HTTPS_PROXY` = `Testing:OutboundProxy` (dead port by default, recording proxy in fixtures), `NO_PROXY=localhost,127.0.0.1` |
| Local JWT trust | `devtoken.json` + key from AppHost user secrets | settings applied in-process by the fixture | AppHost arguments from the fixture (new key per run) |
| Ports | launch profiles | random (testing builder) | random |

Differences are expressed with configuration, `if (mode …)` blocks around **additions** (seed path, test-only
resources, test environment variables), and one helper that adds an application per mode. Do not copy the topology
into per-mode branches, and do not force one value on every mode when isolation needs different ones (keys, issuers,
audiences, environment names).

`DistributedApplicationTestingBuilder` runs the AppHost entry point inside the test process: the arguments a fixture
passes (including this run's signing key) never appear on an OS command line. Local mode and the test modes can run
at the same time (verified: functional and component tests passed while the local AppHost was serving requests),
because every test run gets its own containers and random ports.

## The contract

`AppHostContract.cs` lives in the AppHost folder and is the single source of truth shared with the tests:
mode key and values, resource names, the configuration keys the applications read, the local JWT configuration keys
and, per application, the **JWT trust mapping** (`local-jwt-authentication.md` § Trust mapping).

- Compiled into the AppHost as `internal` (`<DefineConstants>$(DefineConstants);APPHOST</DefineConstants>`) and linked
  into `tests/{App}.Tests.Shared` as `public` source
  (`<Compile Include="..\..\local\aspire\{App}.AppHost\AppHostContract.cs" Link="AppHostContract.cs" />`). Test
  projects that reference both see one public type; nothing in `local/` depends on `tests/`.
- *Changed:* replaces the former `TestHostContract` (which lived in Shared and was linked the other way, and used the
  key `TestMode`).
- Adding a resource name, a topic or a configuration key is an allowed additive change in incremental mode.

## Creating the AppHost

Check the solution's TFM, then choose the first method that applies. All produce
`local/aspire/{App}.AppHost/{App}.AppHost.csproj`. Behaviour verified with Aspire CLI 13.6.1; re-check `--help` on the
installed version.

1. **`dotnet new aspire-apphost`** when the Aspire templates are installed (`dotnet new list aspire-apphost`):
   `dotnet new aspire-apphost -n {App}.AppHost -o local/aspire/{App}.AppHost -f <tfm>`. It creates `AppHost.cs`, the
   csproj, `Properties/launchSettings.json`, `appsettings*.json` and `aspire.config.json`, and does not touch the
   solution.
2. **Hand-written project** when the templates are not installed, to avoid a user-level install: the csproj in
   `architecture-and-layout.md` § Project files (latest stable `Aspire.AppHost.Sdk` matching the CLI channel;
   `aspire integration search postgres` shows the current stable version, NuGet is authoritative), `AppHost.cs` from
   the asset, and `Properties/launchSettings.json` with an `https` (optionally `http`) profile setting
   `applicationUrl`, `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT=Development`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` and
   `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` with unused local ports. The SDK adds `Aspire.Hosting.AppHost` implicitly: do
   not add it as a package. `AspireUseCliBundle` makes the AppHost use the CLI's orchestration bundle, so developers
   need the Aspire CLI: document it.
3. **Install the templates** (`dotnet new install Aspire.ProjectTemplates::<version>`) and use method 1, only with
   user consent: it is a user-level change.

Add a `UserSecretsId` in every case: local keys and external credentials live in the AppHost user secrets.

Do **not** use these in a repository with a solution:

- `aspire new aspire-empty` creates a file-based `apphost.cs`, which cannot be a solution project.
- `aspire init` (13.6.1) creates `{App}.AppHost/` at the repository root, does not add it to the solution, installs
  `Aspire.ProjectTemplates` into the user's template store and tries to trust the HTTPS development certificate. If
  the user prefers it anyway, pass `--suppress-agent-init` (agent setup is a later phase), move the project to
  `local/aspire/` before its first run and add it to the solution.

## Hosting integrations

```bash
aspire integration search <term> --format Json                # exact package ids and versions
aspire add <exact.package.id> --apphost <AppHost.csproj> --non-interactive
```

In non-interactive mode `aspire add` needs the exact package id or friendly name, otherwise it fails with exit code 5
("No exact match"). Respect CPM: move a written `Version` to `Directory.Packages.props`. Stable versions only; a
preview package needs explicit consent and a note in the docs.

## Modelling resources

- **Evidence only.** Every resource is justified by the dependency inventory (`discovery.md` § 4). Keep it small.
- **Same engine as production**, through official hosting integrations (`AddPostgres`, `AddSqlServer`, `AddRedis`,
  `AddRabbitMQ`, `AddKafka`, `AddAzureStorage().RunAsEmulator()`…). `AddContainer` with a pinned tag only when no
  integration exists; `AddDockerfile` when the repository depends on its Dockerfile.
- **Ephemeral data in every mode** (SKILL.md rule 7): no `WithDataVolume`, `WithDataBindMount`,
  `WithLifetime(ContainerLifetime.Persistent)` or `WithPersistentLifetime` on databases, caches, brokers or storage
  emulators.
- **Name resources after the configuration keys the code already reads.** `WithReference(db)` on a resource named
  `orders` injects `ConnectionStrings__orders` (plus `ORDERS_*` properties in 13.x). Use
  `WithReference(db, connectionName: "<Key>")` or `WithEnvironment("Section__Key", value)` when the name cannot match.
- **`WithReference` is not readiness.** `WaitFor(x)` for anything that must be healthy first;
  `WaitForCompletion(dbInitializer)` for one-shot jobs.
- **Health.** `WithHttpHealthCheck("/health")` only if the endpoint exists in the environment the mode gives the app
  (ServiceDefaults maps it in Development only). A check that never passes stalls startup until a timeout.
- **Recent APIs.** `WithExplicitStart`, `ResourceCommands`, `WithReference(..., connectionName)` and
  `ParameterResource.GetValueAsync` are recent additions: verify them against the installed Aspire version
  (`aspire docs api search`) and adapt rather than downgrade the design.
- **An application that migrates itself** gets no second migration runner (`database.md` § When the application
  migrates itself).
- **Launch profile (Local).** Aspire picks `launchProfileName` from `AddProject`, then the profile named like the
  AppHost's (`https`), then the first profile. It supplies `ASPNETCORE_ENVIRONMENT` and the URLs: check that it sets
  Development, pass `launchProfileName:` explicitly when names don't line up, verify with
  `aspire describe <res> --format Json` (`properties.project.launchProfile`). Test modes use `launchProfileName: null`.
- **Endpoints.** No hardcoded ports, IPs or container addresses. Local mode keeps the launch-profile endpoints; test
  modes get dynamic ones and clients come from `App.CreateHttpClient(resource, "http")`.
- **Projects run as projects.** Do not containerize .NET projects that `AddProject` can run.
- **External services.** `AddParameter`/`AddConnectionString` for values developers supply in Local mode; WireMock or
  an emulator in test modes (`external-services.md`).
- **One owner per piece of infrastructure.** If Compose also starts the same database, decide which owns local
  development and document it; do not run both against the same ports and data.
- **Local JWT trust** is applied per application with `WithLocalJwtTrust(...)` from the contract's mapping
  (`local-jwt-authentication.md`). Never inject production keys, issuers or authorities.

Shape (the full, commented version is `assets/local/apphost/AppHost.cs`):

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var mode = builder.Configuration[ModeKey] ?? Modes.Local;
var isTest = mode != Modes.Local;

var postgres = builder.AddPostgres(Resources.Postgres);              // ephemeral: no volume, session lifetime
var ordersDb = postgres.AddDatabase(Resources.OrdersDb);

var dbInitializer = builder.AddProject<Projects.Contoso_Orders_DbInitializer>(Resources.DbInitializer)
    .WithReference(ordersDb, connectionName: AppSettings.OrdersDbConnectionName).WaitFor(ordersDb);
if (!isTest)
{
    dbInitializer.WithEnvironment("Seed__Path", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../database/seed")));
}
else
{
    dbInitializer.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty);   // no telemetry in tests
}

if (mode != Modes.Component)            // component tests host the API in-process
{
    var localJwt = LocalJwt.Create(builder, requireKeys: !isTest);
    AddApplication<Projects.Contoso_Orders_Api>(Resources.OrdersApi)    // per-mode endpoints, environment, safety net
        .WithReference(ordersDb, connectionName: AppSettings.OrdersDbConnectionName)
        .WaitFor(ordersDb).WaitForCompletion(dbInitializer)
        .WithLocalJwtTrust(localJwt, Resources.OrdersApi);
}

builder.Build().Run();
```

## Configuration and secrets

- Let Aspire inject connection information through `WithReference`; match the keys the application already reads.
- Do not duplicate generated values in `appsettings*.json`. Leave existing developer connection strings untouched
  unless they conflict, and explain any change (Aspire's environment variables override them at runtime).
- Local data stores use the credentials Aspire generates: they only need to be valid for the current run, so do not
  pin them.
- Local secrets (external credentials, the local JWT key) go through `AddParameter(name, secret: true)` with values
  from the AppHost user secrets (`aspire secret set Parameters:<name> <value> --apphost <csproj>`, or `devtoken init`
  for the JWT key) or environment variables (`Parameters__<name>`). Resolution order: environment variable, then
  configuration/user secrets, then a dashboard prompt. Never commit them.
- Sanitized examples only, under `local/` (e.g. `local/aspire/{App}.AppHost/appsettings.Local.example.json`).

## Repairing an existing AppHost

- Keep its name, location (unless it violates the `src/`/`local/` separation and the user agrees to move it),
  resource names and parameters. Renaming resources changes connection keys.
- Moving it: `git mv`, fix `ProjectReference` relative paths, `aspire.config.json`, solution entries (remove + add),
  scripts, docs and CI, together and with consent.
- Persistence in the model (`WithDataVolume`, bind mounts, persistent lifetimes) violates the ephemeral rule: remove
  it, add the schema + seed initializer if missing, list the volumes and persistent containers it created by name and
  leave them for the user to delete (they may hold data the developer wants to export first).
- Fix missing `WaitFor`, hardcoded connection strings, missing solution entries and deployable projects that
  reference the AppHost, one by one, each with its evidence.
- Add the mode switch and the contract if they are missing, then consolidate any test AppHost (next section).

## Consolidating a dedicated test AppHost

Repositories built with the former testing skill (or by hand) often have `tests/orchestration/{App}.Tests.AppHost/`
or `tests/{App}.TestAppHost/` next to `local/.../{App}.AppHost/`. Rule 4 makes one of them redundant. With consent
(it changes test infrastructure), in this order:

1. Inventory both models side by side: resources, names, configuration keys injected, third-party substitutes,
   identity stubs, mode switch, health checks, explicit starts.
2. Move into the shared AppHost only what tests need and Local lacks, as test-mode additions (WireMock resources,
   proxy variables, explicit start, test environment name). Keep the shared resource names; when the test AppHost used
   different names, rename in the test code (contract), not in the topology.
3. Replace `TestHostContract` with `AppHostContract` (keys, resource names, topics), relink it from the AppHost
   folder, and add `Aliases="apphost"` to the AppHost references of component/functional projects.
4. Point the fixtures to the shared AppHost type and `--RunMode=…`; move schema application to the DbInitializer
   (`database.md` § Schema in test modes).
5. Replace the test token trust by the configuration mapping (`local-jwt-authentication.md`); keep the identity stub
   only if the decision tree there still requires it.
6. Remove the test AppHost project from the solution and the disk only after the whole suite passes on the shared one.
   Remove empty `tests/orchestration/` folders; move any scripts it held to `local/database/` or `local/scripts/`.
7. Run the full validation (`validation-and-checklist.md`) and report what moved where.
