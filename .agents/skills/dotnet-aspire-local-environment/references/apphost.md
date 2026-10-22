# AppHost, solution and resource topology

## Target structure

```text
<repo-root>/
├── <Solution>.sln | <Solution>.slnx          existing, reused
├── src/                              application + shared runtime code (all environments)
│   └── {App}.ServiceDefaults/       only if justified (see observability.md)
├── tests/                            test projects (+ {App}.AspireTests only if useful)
└── local/
    ├── aspire/
    │   ├── {App}.AppHost/           local orchestration (authoritative local topology)
    │   └── {App}.MigrationService/  only if a local-only migration runner is needed
    ├── database/                     local-only init/seed scripts (not the canonical schema)
    ├── scripts/                      local-only helper scripts
    └── local-development.md
```

## Project naming: `{App}.<Role>`

Every project this skill creates is named after the application: `{App}.<Role>`, and the folder name equals the
project name.

| Role | Folder / project |
|---|---|
| AppHost | `local/aspire/{App}.AppHost/{App}.AppHost.csproj` |
| Local migration runner | `local/aspire/{App}.MigrationService/{App}.MigrationService.csproj` |
| Shared service defaults | `src/{App}.ServiceDefaults/{App}.ServiceDefaults.csproj` |
| Aspire application-model tests | `tests/{App}.AspireTests/{App}.AspireTests.csproj` |

How to determine `{App}`, in order:

1. The common prefix of the existing application projects: `Contoso.Orders` when they are `Contoso.Orders.Api`,
   `Contoso.Orders.Domain`, and so on. Keep its exact casing and dots.
2. If the projects have no common prefix (for example `Api`, `Worker`), the solution file name (`Orders.slnx` gives
   `Orders`).
3. If neither is clear (several candidate prefixes, a generic solution name), ask the user and report the choice in
   the plan.

Never use generic names (`AppHost`, `MyApp.AppHost`, `Repository.AppHost`) or the repository folder name when it
differs from the application name. A `dotnet new`, `aspire init` or `aspire new` template with another name must be
created with `-n {App}.<Role>`, or renamed before it is added to the solution. In repair mode, keep existing project
names: renaming an existing AppHost needs the user's consent, like moving it does.

The generated `Projects.*` type replaces dots with underscores. For example `Contoso.Orders.Api` becomes
`Projects.Contoso_Orders_Api`.

## Folder hygiene

Do not create empty folders: `local/database/` and `local/scripts/` exist only when they hold something. Make sure `local/` is not ignored by `.gitignore`: `aspire ls` skips git-ignored directories.

## Creating the AppHost

Check the solution's TFM, then choose the first method that applies. All three produce
`local/aspire/{App}.AppHost/{App}.AppHost.csproj`. Behaviour below was verified with Aspire CLI 13.6.1; re-check
`--help` on the installed version.

1. **`dotnet new aspire-apphost`** when the Aspire templates are installed (`dotnet new list aspire-apphost`):

   ```bash
   dotnet new aspire-apphost -n {App}.AppHost -o local/aspire/{App}.AppHost -f <tfm>
   ```

   Creates `AppHost.cs`, the csproj, `Properties/launchSettings.json`, `appsettings*.json` and `aspire.config.json`.
   It does not touch the solution.

2. **Hand-written project** when the templates are not installed, to avoid a user-level install. Use the latest
   stable `Aspire.AppHost.Sdk` version that matches the CLI channel (`aspire integration search postgres` shows the
   current stable Aspire version; NuGet is authoritative):

   ```xml
   <Project Sdk="Aspire.AppHost.Sdk/<version>">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFramework><tfm></TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
       <AspireUseCliBundle>true</AspireUseCliBundle>
       <UserSecretsId><new GUID></UserSecretsId>
     </PropertyGroup>
   </Project>
   ```

   plus `AppHost.cs` (`var builder = DistributedApplication.CreateBuilder(args); … builder.Build().Run();`) and
   `Properties/launchSettings.json` with an `https` (and optionally `http`) profile that sets `applicationUrl`,
   `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT=Development`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` and
   `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` with unused local ports. The SDK adds `Aspire.Hosting.AppHost` implicitly.
   Do not add it as a package. `AspireUseCliBundle` makes the AppHost use the CLI's orchestration bundle (current
   templates set it). Developers then need the Aspire CLI, so document it.

3. **Install the templates** (`dotnet new install Aspire.ProjectTemplates::<version>`) and use method 1, only with
   user consent, because it is a user-level change.

Do **not** use these for a repository with a solution:

- `aspire new aspire-empty` creates a file-based `apphost.cs`, which cannot be a solution project.
- `aspire init` (13.6.1) creates `{App}.AppHost/` at the repository root and does not add it to the solution. It
  also installs `Aspire.ProjectTemplates` into the user's template store and tries to trust the HTTPS development
  certificate. If the user prefers it anyway, pass `--suppress-agent-init` (agent setup is Phase 7), then move the
  generated project to `local/aspire/` before its first run and add it to the solution.

## Adding projects to the solution

```bash
dotnet sln <Solution>.slnx add --solution-folder local local/aspire/{App}.AppHost/{App}.AppHost.csproj
dotnet sln <Solution>.slnx add --solution-folder src src/{App}.ServiceDefaults/{App}.ServiceDefaults.csproj
dotnet sln <Solution>.slnx list
```

- Use the same command for `.sln`. Mirror existing folder names and casing. Put every new local project (AppHost,
  migration runner) in one `local` folder unless the solution already nests folders by path. `--solution-folder src`
  adds to the existing `/src/` folder rather than creating a duplicate (verified with `.slnx`). `--solution-folder local/aspire`
  creates nested folders, `--in-root` puts the project at the root. The two options are mutually exclusive.
- `dotnet sln` only manages projects. To show `local/local-development.md` or scripts as solution items, edit the
  solution file by hand only if the repository already uses solution items; otherwise leave them on disk under
  `local/`.
- Verify: every new csproj appears in `dotnet sln … list`, and `dotnet build <solution>` builds it.

## Project references

```bash
dotnet add local/aspire/{App}.AppHost reference src/{App}.Api   # generates Projects.{App}_Api
```

- The AppHost references executable projects only. The SDK marks them non-runtime references, so nothing from them
  is copied into the AppHost.
- `src/` projects never reference `local/` projects. Check with
  `grep -rn --include='*.csproj' 'local[/\\]' src`.
- New local projects that consume shared libraries must resolve the same package versions as the existing consumers.
  For example, when a library gets EF Core through a `PrivateAssets="all"` package such as
  `Microsoft.EntityFrameworkCore.Design`, a new consumer can resolve an older transitive EF Core and fail with
  `CS1705`. Fix it with CPM or an explicit matching `PackageReference`, never by upgrading the application.

## Hosting integrations

```bash
aspire integration search <term> --format Json                # find exact package ids and versions
aspire add <exact.package.id> --apphost <AppHost.csproj> --non-interactive
```

In non-interactive mode, `aspire add` needs the exact package id or friendly name. Otherwise it fails with exit code 5
("No exact match"). Respect CPM: if `aspire add` writes a `Version` attribute in a CPM repository, move the version
to `Directory.Packages.props`. Use stable versions. A preview package needs explicit user consent and a note in the
docs.

## Modelling resources

Rules:

- **Name resources after the configuration keys the code already reads.** `WithReference(db)` on a resource named
  `orders` injects `ConnectionStrings__orders`, plus `ORDERS_*` properties in 13.x. Use `connectionName` overloads or
  `WithEnvironment` only when the key cannot match.
- **`WithReference` ≠ readiness.** Add `WaitFor(x)` for anything that must be up and healthy before start, and
  `WaitForCompletion(job)` for one-shot initializers (migrations, seeders).
- **Health.** Give HTTP services `WithHttpHealthCheck("/health")` only if that endpoint exists in the environment
  the AppHost runs them in. The ServiceDefaults template maps it in Development only.
- **Launch profile.** Aspire picks each project's profile in this order: `launchProfileName` in `AddProject`, then the
  profile with the same name as the AppHost's (`https` in the generated AppHost), then the project's first profile.
  The profile supplies `ASPNETCORE_ENVIRONMENT` and the URLs, so check that the chosen one sets Development.
  Pass `launchProfileName:` explicitly when the names don't line up. Verify in `aspire describe <res> --format
  Json` (`properties.project.launchProfile`).
- **Endpoints.** Let projects keep their launch-profile endpoints. Do not hardcode ports, IPs or container addresses.
- **Containers.** Use official hosting integrations (`AddPostgres`, `AddSqlServer`, `AddRedis`, `AddRabbitMQ`,
  `AddKafka`, `AddAzureStorage().RunAsEmulator()`…). Fall back to `AddContainer` with a pinned image tag only when
  no integration exists. Reuse an existing Dockerfile with `AddDockerfile` when the repository depends on it.
- **Projects run as projects.** Do not containerize .NET projects that `AddProject` can run directly.
- **External services.** `AddParameter`/`AddConnectionString` for values developers supply, documented in
  `local/local-development.md`.
- **One owner per piece of infrastructure.** If Compose also starts the same database, decide which owns local
  development and document it. Do not run both against the same ports and data.
- **Keep it small.** Only resources the inventory justifies.

Example shape (adapt to the inventory; every line must be justified by evidence):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var ordersDb = postgres.AddDatabase("orders");            // key the app reads: ConnectionStrings:orders

var cache = builder.AddRedis("cache");

var migrations = builder.AddProject<Projects.Contoso_Orders_MigrationService>("migrations")
    .WithReference(ordersDb)
    .WaitFor(ordersDb);

builder.AddProject<Projects.Contoso_Orders_Api>("api")
    .WithReference(ordersDb).WaitFor(ordersDb)
    .WithReference(cache).WaitFor(cache)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
```

## Configuration and secrets

- Generated container passwords are stored in the AppHost's user secrets after the first run. Persistent volumes
  depend on that password staying stable. To pin one explicitly:
  `var pw = builder.AddParameter("postgres-password", secret: true); builder.AddPostgres("postgres", password: pw)`,
  with `aspire secret set Parameters:postgres-password <value> --apphost <csproj>` (or env
  `Parameters__postgres_password`).
- Parameter resolution order: environment variable `Parameters__<name>`, then configuration or user secrets
  `Parameters:<name>`, then a dashboard prompt.
- Sanitized examples only, under `local/`. Add populated local files to `.gitignore` when the repository uses them.

## Repairing an existing AppHost

- Keep its name, location (unless it violates the `src/`/`local/` separation and the user agrees to move it),
  resource names, parameters and volumes. Renaming resources changes generated volume names and connection keys.
- Moving it: `git mv`, fix `ProjectReference` relative paths, `aspire.config.json`, solution entries (remove + add),
  scripts, docs and CI. Persistent containers and volumes are tracked by AppHost path, so they are orphaned and
  recreated empty. Warn about that before moving.
- Fix missing `WaitFor`, hardcoded connection strings, missing solution entries and deployable projects that
  reference the AppHost, one by one, each with its evidence.
