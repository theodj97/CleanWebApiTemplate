# Clean Web API Template

`dotnet new` template for an ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT, JWT authentication,
PostgreSQL, OpenBao and Aspire. This repository is both the template source and a working solution: everything under
`src/`, `tests/` and `local/` builds, runs and is tested as is.

What a generated project gets:

- Minimal API published as **Native AOT** (no reflection, source-generated JSON and request delegates).
- Clean Architecture (`Api`, `Application`, `Domain`, `Infrastructure`, `ServiceDefaults`) enforced by architecture
  tests and banned APIs.
- JWT authentication with two explicit modes: `Symmetric` (HS256, local and tests) and `Oidc` (deployed).
- PostgreSQL through Npgsql, with a unique index per owner and parameterised queries.
- Secrets read from **OpenBao** at startup; no secrets or connection strings in `appsettings*.json`.
- **Aspire** local environment: ephemeral PostgreSQL and OpenBao, schema + seed on every start, a Swagger sidecar with
  examples and a profile picker, and the dashboard with logs and traces.
- Unit, component, functional and architecture tests on Microsoft Testing Platform, with coverage thresholds.
- Husky.Net hooks: gitleaks + format on `pre-commit`, conventional subjects on `commit-msg`, full gates on `pre-push`.

## Use the template

### From NuGet

Once the package is published:

```bash
dotnet new install CleanWebApiTemplate             # latest published version
dotnet new install CleanWebApiTemplate::1.0.0      # a pinned version
dotnet new clean-webapi -n MyCompany.Orders -o MyCompany.Orders
cd MyCompany.Orders
git init
dotnet husky install
```

Until the first publish, use the source install below.

### From source

```bash
dotnet new install .                       # from a clone of this repository
dotnet new clean-webapi -n MyCompany.Orders -o MyCompany.Orders
cd MyCompany.Orders
git init
dotnet husky install
```

| Option | Effect |
|---|---|
| `-n`, `--name` | Replaces `CleanWebApiTemplate` in file names, namespaces and content (lower case forms too). |
| `--no-restore` | Skips the NuGet and `dotnet tool restore` post-actions. |
| `--allow-scripts yes` | Runs `dotnet tool restore` without prompting (it is a script post-action). |

Post-actions: restore NuGet packages, `dotnet tool restore` and, on Linux/macOS, `chmod +x` for the hook scripts.
`git init` and `dotnet husky install` stay manual: `dotnet new` does not create a Git repository on your behalf.

Every generation gets new `UserSecretsId` values for the API and the AppHost, so two generated projects never share
user secrets. The generated project ships its own [`README.md`](README.template.md), `.gitignore`, `AGENTS.md`,
hooks and scripts; it does not include this repository's audit (`docs/audit/`), coverage reports, local tools, agent
skills or the NuGet packaging project (`build/`).

Uninstall the package with `dotnet new uninstall CleanWebApiTemplate`, or the source install with
`dotnet new uninstall <path-to-this-repository>`.

## Work on this repository

Requirements and first run are the same as for a generated project (see
[`README.template.md`](README.template.md#quickstart)):

```bash
dotnet tool restore && dotnet husky install
dotnet local/tools/devtoken.cs init
aspire start
```

| Command | What it does |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | Build, 0 warnings (`TreatWarningsAsErrors`). |
| `dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained` | Native AOT publish, 0 warnings. |
| `dotnet test --solution CleanWebApiTemplate.slnx` | All tests (Docker for component/functional). |
| `npm run verify` | Same gates as `pre-push`. |
| `npm run coverage` | Coverage report and thresholds. |
| `bash scripts/template-smoke.sh` | Installs the template in an isolated hive, generates two projects and checks them. |
| `dotnet pack build/CleanWebApiTemplate.Template.csproj -c Release -o artifacts` | Builds the NuGet template package. |
| `bash scripts/package-smoke.sh` | Packs the template, installs the `.nupkg` in an isolated hive, generates and validates a project. |

### Template smoke test

`scripts/template-smoke.sh` validates the template end to end without touching your global template list:

1. Installs this folder in a temporary `--debug:custom-hive`.
2. Generates `Sample` (with restore) and `Sample2` (`--no-restore`).
3. Checks the output: `README.md` and `.gitignore` present; no `coverage-report/`, `.agents/`, `.tools/`,
   `docs/audit/`, `skills-lock.json`, `README.template.md`, `build/`, `CHANGELOG.md`, `LICENSE`, `README.nuget.md` or
   `scripts/package-smoke.sh`; no `CleanWebApiTemplate`/`cleanwebapitemplate` left in names or content; distinct
   `UserSecretsId` per project and per generation; executable hook scripts.
4. Runs `git init` + `dotnet husky install` and checks `core.hooksPath`.
5. Builds the solution, runs the unit and architecture tests and publishes the API as Native AOT.

Options: `--keep` keeps the output directory, `--no-aot` skips the AOT publish. The script itself is excluded from
the generated project.

### Package smoke test

`scripts/package-smoke.sh` validates the NuGet package end to end:

1. Packs `build/CleanWebApiTemplate.Template.csproj` into a temporary `artifacts/` directory.
2. Inspects the `.nupkg`: the template config, `README.template.md`, source, tests and local projects are present;
   `build/`, audit, coverage, agent skills and the smoke scripts are absent; the package readme, license and
   changelog are at the package root.
3. Installs the `.nupkg` in a temporary `--debug:custom-hive`, generates `Sample`, and checks the generated files.
4. Builds the generated solution, runs the unit, architecture and sidecar tests, and publishes Native AOT unless
   `--no-aot` is passed.
5. Uninstalls the package from the isolated hive.

Options: `--keep` keeps the work directory, `--no-aot` skips the AOT publish.

### NuGet package

The package is built from `build/CleanWebApiTemplate.Template.csproj`:

```bash
dotnet pack build/CleanWebApiTemplate.Template.csproj -c Release -o artifacts
dotnet nuget push artifacts/CleanWebApiTemplate.1.0.0.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

The package ID is `CleanWebApiTemplate`, the version is in the packaging project, and the license is MIT. If the ID
is already taken on NuGet.org, change `PackageId` in the packaging project (the `shortName` stays `clean-webapi`).
For a private feed, publish the same `.nupkg` to GitHub Packages or Azure Artifacts and install it with
`dotnet new install CleanWebApiTemplate --nuget-source <feed>`.

## Documentation

| Document | Content |
|---|---|
| [`README.template.md`](README.template.md) | README of the generated project: quickstart, architecture, configuration, API contract, tests and hooks. |
| [`local/local-development.md`](local/local-development.md) | Aspire topology, OpenBao, local JWTs, database, Swagger sidecar and troubleshooting. |
| [`tests/README.md`](tests/README.md) | Test projects, identities, conventions and coverage. |
| [`local/openapi/README.md`](local/openapi/README.md) | Swagger sidecar, examples catalog and profile picker. |
| [`AGENTS.md`](AGENTS.md) | Rules for coding agents: Native AOT constraints and hooks. |
| [`README.nuget.md`](README.nuget.md) | Install and quickstart displayed on NuGet.org for the template package. |
| [`build/CleanWebApiTemplate.Template.csproj`](build/CleanWebApiTemplate.Template.csproj) | NuGet template packaging project (`PackageType=Template`). |
| [`CHANGELOG.md`](CHANGELOG.md) | Version history of the template package. |
| [`docs/audit/`](docs/audit/README.md) | Audit of this repository and the remediation batches (not shipped with the template). |
