---
name: dotnet-aspire-local-environment
description: >-
  Audits, creates, repairs and validates the local development environment of an existing .NET repository with
  Aspire: an AppHost under local/aspire/ added to the existing .sln/.slnx, evidence-based infrastructure resources,
  database initialization (EF Core migrations, scaffolding, Dapper/SQL, mixed), dashboard observability, the official
  `aspire agent init` skills, and an evidence-first debugging loop using Aspire CLI logs and traces. Invoke explicitly
  with /dotnet-aspire-local-environment [audit|setup|repair|debug <symptom>].
disable-model-invocation: true
argument-hint: "[audit | setup | repair | debug <symptom>]"
compatibility: Needs the repository's .NET SDK, the Aspire CLI (verified against 13.6.x) and Docker or Podman for container resources.
---

# .NET Aspire local development environment

You turn an existing .NET repository into one that starts locally with a single Aspire command, with real
infrastructure, a correctly initialized database, meaningful dashboard telemetry and AI-agent-ready runtime evidence.
You have never seen this repository: everything comes from evidence in it, never from a template.

## Mode

Read `$ARGUMENTS`:

| Argument | Mode | Stops after |
|---|---|---|
| `audit` | Discovery and gap report only. No file changes. | Phase 2 |
| `setup` or empty, and no AppHost exists | Full creation. | Phase 10 |
| `repair` or empty, and an AppHost exists | Reconcile the existing environment with the rules below. | Phase 10 |
| `debug <symptom>` | Runtime diagnosis of a reported failure (`references/agent-integration.md` § Debugging loop). | Verified fix or honest report |

## Non-negotiable rules

1. **Folder responsibilities.** `src/` holds application and shared runtime code for every environment. `tests/`
   holds test projects and test resources. `local/` holds everything local-only: the AppHost under
   `local/aspire/{App}.AppHost/`, local-only support projects, `local/database/`, `local/scripts/` and
   `local/local-development.md`. This is a repository convention, not an Aspire requirement. Never restructure
   existing application code to fit it.
   **Every project you create is named after the application, `{App}.<Role>`**, with a folder of the same name:
   `{App}.AppHost`, `{App}.MigrationService`, `{App}.ServiceDefaults`, `{App}.AspireTests`. `{App}` is the existing
   projects' common prefix (`Contoso.Orders` from `Contoso.Orders.Api`), or else the solution name. If it is
   ambiguous, ask. Never use generic template names (`AppHost`, `MyApp.AppHost`). See `references/apphost.md`
   § Project naming.
2. **No deployable code depends on `local/`.** Projects in `src/` must build and deploy without anything under
   `local/`. Code that application projects need at runtime (for example a ServiceDefaults project) is shared runtime
   code and lives in `src/` or the repository's shared-code location.
3. **One solution.** Discover the authoritative `.sln`/`.slnx` before creating any project. Add every new `.csproj`
   to it with `dotnet sln <solution> add`, respecting existing solution folders. Never create a second solution,
   never convert between `.sln` and `.slnx`.
4. **Evidence over assumption.** A dependency exists when code, configuration or infrastructure files use it, not
   because a package is referenced. Every API, CLI option and package version is verified against the installed
   tooling (`<command> --help`, `aspire docs api search`, `aspire integration search`) before use.
5. **Minimal, reversible changes.** Preserve architecture, names, namespaces, migration history, existing
   infrastructure and user modifications. No unrelated upgrades, migrations or reorganizations. Never delete and
   recreate an existing AppHost because it differs from a template.
6. **Data safety.** Never drop databases, delete volumes, recreate data stores or connect to production. In
   particular never run `aspire stop --force --volumes`, `docker volume rm` or `dotnet ef database drop` without
   explicit user consent for that specific operation (`references/database.md` § Safety).
7. **Secrets.** Never commit or print passwords, tokens, connection strings or dashboard login URLs. `aspire describe
   --format Json` includes resource environment variables with generated credentials, and `aspire start`/`aspire ps`
   return a `dashboardUrl` containing the login token: redact them before showing any output.
8. **No machine-wide side effects without consent.** Ask before installing global tools, templates or npm packages,
   trusting certificates, or touching user-level Claude/agent configuration. See the side effects listed in
   `references/apphost.md` § Creating the AppHost and `references/agent-integration.md`.
9. **Truthful reporting.** Report real commands with real exit codes. Do not pipe a command through `tail`/`grep`
   in a way that hides its exit code when the result matters. Never claim that a build, migration, startup, test or
   telemetry check succeeded unless you ran it and saw it succeed. Distinguish confirmed findings from hypotheses.

## Workflow

### Phase 0 — Tooling and workspace state

Run and record. Versions go into the final report and `local/local-development.md`; in `audit` mode only the report:

```bash
git status --short
dotnet --info
aspire --version
docker info --format '{{.ServerVersion}}'   # or: podman info
aspire ps --format Json                      # AppHosts already running: never stop or duplicate them
dotnet new list aspire-apphost               # Aspire templates installed? (decides how the AppHost is created)
```

Read `CLAUDE.md`, `AGENTS.md` and equivalent instruction files, and honor them. Note uncommitted user changes and do
not overwrite them. If the Aspire CLI is missing, continue with discovery and file work, and report the official
installation instructions (`references/sources.md`). If no container runtime is available, say so immediately:
files can be created and built, but container resources cannot be started or verified. On Podman, Aspire needs
`ASPIRE_CONTAINER_RUNTIME=podman`.

### Phase 1 — Discovery and inventory

Follow `references/discovery.md`. Produce:

- The authoritative solution file and format, all projects, SDK/TFMs, CPM and shared build properties.
- The application name `{App}` for new projects (common project prefix, else solution name).
- Executable components (APIs, workers, web apps, gRPC, scheduled jobs) and their project references.
- Existing Aspire state: **missing**, **partially configured**, **configured incorrectly** or **compliant**.
- A dependency inventory, each item classified as *runtime-required*, *development-only*, *test-only*, *external
  (not reasonably replaceable locally)*, *optional* or *unused* (package referenced but never used: reported, not
  provisioned), with the evidence (file:line) for each.
- For every database: engine, provider, accessing projects, data-access strategy, authoritative schema source,
  existing initialization mechanism, persistence needs.
- Existing telemetry, health checks, service discovery, resilience and local-development docs/scripts.
- Existing agent skill locations and `.mcp.json`, and secrets already committed (reported with values redacted).

Discovery is read-only in every mode: no builds, restores or database connections. Search with `git grep` so that
build output is not matched.

### Phase 2 — Gap report and plan

Present the inventory, the gaps against the rules above and a concise implementation plan: the `{App}` name and
where it comes from, files to create or change, projects to add to which solution folder, the database strategy (A–E in `references/database.md`) with its
justification, the decisions that need the user (preview packages, global installs, moving an existing AppHost,
external credentials).

`audit` mode ends here. In `setup`/`repair`, **wait for confirmation** before substantial changes. Safe independent
work (reading, building, running existing tests) may continue.

### Phase 3 — Structure, AppHost and solution

Follow `references/apphost.md`:

- Create the AppHost at `local/aspire/{App}.AppHost/` as a project-based C# AppHost (unless the repository already
  uses another supported AppHost style), or repair the existing one in place. Moving an existing AppHost requires
  updating project paths, references, solution entries, scripts and docs together, and orphans its persistent
  resources: get consent first.
- Add the AppHost and every other new project to the existing solution, then verify with `dotnet sln <solution> list`.
- Model the topology: projects, databases, caches, brokers, storage emulators, containers, with `WithReference` for
  configuration and `WaitFor`/`WaitForCompletion` for readiness. No hardcoded hosts, ports or connection strings.
- Decide on ServiceDefaults from evidence (Phase 6), never just because a template has one.

### Phase 4 — Database initialization

Follow `references/database.md`. Pick exactly one authoritative schema-management strategy per database (A: EF Core
migrations, B: EF Core database-first/scaffolding, C: Dapper/ADO.NET/SQL, D: mixed, E: no authoritative source). Make
schema initialization complete before dependent resources start, give it durable local storage, keep destructive
resets separate from startup, and seed only synthetic data.

### Phase 5 — Configuration and secrets

- Let Aspire inject connection information through `WithReference`; match the configuration keys the application
  already reads (for example `ConnectionStrings:<name>` → name the Aspire resource `<name>`).
- Do not duplicate generated values in `appsettings*.json`. Leave existing developer connection strings untouched
  unless they conflict, and explain any change.
- Local secrets go through `AddParameter(name, secret: true)` with values from AppHost user secrets
  (`aspire secret set Parameters:<name> <value> --apphost <csproj>`) or environment variables. Never commit them.
- If local configuration needs files, commit only sanitized examples under `local/` (for example
  `local/aspire/{App}.AppHost/appsettings.Local.example.json`) and document the required values.
- List external services and the credentials they need; do not fake services that cannot be reasonably emulated.

### Phase 6 — Observability

Follow `references/observability.md`. Reuse the repository's existing telemetry setup when it exists; otherwise add a
ServiceDefaults project under `src/` and wire `AddServiceDefaults()`/`MapDefaultEndpoints()` into each service. Add
dependency instrumentation (database, cache, broker) where the dashboard would otherwise show only inbound HTTP spans.
Never create duplicate logging or tracing pipelines.

### Phase 7 — Official Aspire AI agent integration

Follow `references/agent-integration.md`. Check the CLI supports `aspire agent init` and its options, detect existing
Aspire skill installations in every location, and run it **from the repository root with `--workspace-root <repo
root>`**, using an explicit skill list. MCP stays off unless it brings a concrete benefit and the user opts in.

### Phase 8 — Validation

Follow `references/validation.md`: solution inclusion, restore, build, existing tests, AppHost start, resource
health, fresh-database initialization, a real smoke request, telemetry for that request, agent skill discovery.
Record each command with its exit code. Stop only the AppHosts you started; leave the user's processes and data alone.

### Phase 9 — Documentation

Create or update `local/local-development.md` from `assets/local-development.md`, filling every placeholder with
verified values (commands, resource names, persistence, reset procedure, external services). Do not write dashboard
URLs with tokens, guessed ports or results you did not observe. Respect an existing equivalent document under `local/`.

### Phase 10 — Final report

Report: files created/modified; resulting structure and AppHost path; projects added to the solution (and the
`dotnet sln list` evidence); dependency inventory; database strategy and why; observability changes and what was
verified; `aspire agent init` command, installed skills and locations, MCP decision; validation commands with exit
codes; remaining blockers, assumptions and items needing user action.

## Acceptance criteria

The work is complete only when each item is satisfied or explicitly reported as blocked with the reason:

- [ ] AppHost (and any local-only support project) lives under `local/aspire/`; nothing local-only is in `src/`.
- [ ] Every created project and its folder are named `{App}.<Role>` after the application (for example
      `local/aspire/{App}.AppHost/{App}.AppHost.csproj`); none uses a generic template name.
- [ ] No project in `src/` references anything under `local/`; shared runtime code (ServiceDefaults) is in `src/`.
- [ ] Every new project appears in `dotnet sln <existing solution> list`; no new solution file exists.
- [ ] Every AppHost resource is justified by inventory evidence; references and readiness waits are explicit.
- [ ] Each database has one authoritative schema strategy, initialized before dependants, verified on a fresh
      local database when a container runtime is available.
- [ ] No secret, generated password or dashboard token is committed or printed.
- [ ] Solution builds; existing tests were run and their real result reported.
- [ ] The AppHost started, resources reached the expected state, a smoke request succeeded and its trace/logs were
      found with `aspire otel`/`aspire logs` (or the blocker is reported).
- [ ] `aspire agent init` ran with verified options and the Aspire skills are discoverable at the repository root
      (or the blocker and official instructions are reported). MCP configured only with consent.
- [ ] `local/local-development.md` describes the real implementation.
- [ ] Existing architecture, migration history and user changes are preserved.
