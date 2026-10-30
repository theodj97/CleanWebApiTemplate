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
hooks and scripts; it does not include this repository's audit (`docs/audit/`), coverage reports, local tools or agent
skills.

Uninstall with `dotnet new uninstall <path-to-this-repository>`.

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

### Template smoke test

`scripts/template-smoke.sh` validates the template end to end without touching your global template list:

1. Installs this folder in a temporary `--debug:custom-hive`.
2. Generates `Sample` (with restore) and `Sample2` (`--no-restore`).
3. Checks the output: `README.md` and `.gitignore` present; no `coverage-report/`, `.agents/`, `.tools/`,
   `docs/audit/`, `skills-lock.json` or `README.template.md`; no `CleanWebApiTemplate`/`cleanwebapitemplate` left in
   names or content; distinct `UserSecretsId` per project and per generation; executable hook scripts.
4. Runs `git init` + `dotnet husky install` and checks `core.hooksPath`.
5. Builds the solution, runs the unit and architecture tests and publishes the API as Native AOT.

Options: `--keep` keeps the output directory, `--no-aot` skips the AOT publish. The script itself is excluded from
the generated project.

## Documentation

| Document | Content |
|---|---|
| [`README.template.md`](README.template.md) | README of the generated project: quickstart, architecture, configuration, API contract, tests and hooks. |
| [`local/local-development.md`](local/local-development.md) | Aspire topology, OpenBao, local JWTs, database, Swagger sidecar and troubleshooting. |
| [`tests/README.md`](tests/README.md) | Test projects, identities, conventions and coverage. |
| [`local/openapi/README.md`](local/openapi/README.md) | Swagger sidecar, examples catalog and profile picker. |
| [`AGENTS.md`](AGENTS.md) | Rules for coding agents: Native AOT constraints and hooks. |
| [`docs/audit/`](docs/audit/README.md) | Audit of this repository and the remediation batches (not shipped with the template). |
