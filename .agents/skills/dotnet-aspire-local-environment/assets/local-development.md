<!--
Template for local/local-development.md. Replace every <placeholder> with verified values, delete sections that do
not apply, and never write dashboard login tokens, generated passwords or guessed ports.
-->
# Local development

This repository runs locally with [Aspire](https://aspire.dev). One command starts the application and its
infrastructure, and the Aspire dashboard shows resources, logs, traces and metrics.

## Repository layout

| Folder | Contents |
|---|---|
| `src/` | Application and shared runtime code, deployed to every environment: <projects> |
| `tests/` | Test projects: <projects> |
| `local/` | Local development only: the Aspire AppHost (`local/aspire/{App}.AppHost`)<, migration runner, scripts, init SQL>. Nothing in `src/` depends on it. |

<If a ServiceDefaults project exists:> `src/{App}.ServiceDefaults` is shared runtime code. Services call
`AddServiceDefaults()` in every environment for OpenTelemetry, health checks, service discovery and HTTP resilience.
That is why it lives in `src/`, not in `local/`.

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | <from global.json / verified> | `dotnet --version` |
| Aspire CLI | <verified version> or later | `aspire --version`. Install: <https://aspire.dev/get-started/install-cli/> |
| Container runtime | Docker <version> or Podman (`ASPIRE_CONTAINER_RUNTIME=podman`) | `docker info` |
| HTTPS dev certificate | — | `aspire certs trust` (or `dotnet dev-certs https --trust`) |

## Topology

```text
<resource graph, e.g.
postgres (container, volume) ──► ordersdb ──► migrations (runs once) ──► api
redis (container) ─────────────────────────────────────────────────────► api
>
```

| Resource | Kind | Purpose | Depends on (waits for) |
|---|---|---|---|
| <name> | <Project/Container/Database/Executable> | <purpose> | <deps> |

## Start and stop

From the repository root:

```bash
aspire run --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj     # foreground, Ctrl+C to stop
aspire start --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj   # background
aspire stop --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj    # stops the AppHost; data is kept
```

Running the AppHost project from Visual Studio, Rider or `dotnet run --project local/aspire/{App}.AppHost` also
works.

## Dashboard and endpoints

- **Dashboard:** open the login link printed by `aspire run`/`aspire start`, or get it again with
  `aspire ps --format Json` (`dashboardUrl`). The link contains a personal login token: don't share or commit it.
- **Application endpoints:** listed in the dashboard and in `aspire describe <resource>`. <Verified routes to try,
  e.g. `GET /orders`.>

## Database

- Engine: <engine/version> (Aspire container `<resource>`), database `<name>`, consumed by <projects> through
  `ConnectionStrings:<key>`.
- Schema: <strategy, e.g. "EF Core migrations in src/<…>/Migrations applied by local/aspire/{App}.MigrationService
  before the API starts">.
- Seed data: <synthetic seed source or "none">.
- Persistence: <volume name pattern / "data is lost when the container is removed">. The generated password is
  stored in the AppHost user secrets (`aspire secret list --apphost …`). Keep it, or the volume's data becomes
  unreachable.

### Reset local data (destructive)

Only for your local data. Stop first, then remove only this volume:

```bash
aspire stop --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj
docker volume ls | grep <volume pattern>
docker volume rm <exact volume name>
```

The next start recreates the database and re-runs the schema initialization. <Or: `local/scripts/<reset script>`,
which asks for confirmation.>

### Adding a migration

<e.g. `dotnet ef migrations add <Name> -p src/<migrations project> -s src/<startup project>`>

## Configuration and external services

| Setting | Needed for | How to provide |
|---|---|---|
| <Parameters:x / external API key> | <feature> | `aspire secret set Parameters:<x> <value> --apphost …` or env `Parameters__<x>` |

<Services that stay external and what happens without them.>

## Tests

```bash
dotnet test <solution>
```

<Which test projects need Docker/the AppHost.>

## AI coding agents

The official Aspire agent skills are installed in `<.claude/skills, .agents/skills>` (`aspire agent init`). Refresh
them after upgrading the Aspire CLI:

```bash
aspire agent init --non-interactive --workspace-root . --skills <list> --skill-locations <locations>
```

<MCP: "not configured; the agents use the Aspire CLI" or "configured in .mcp.json (opt-in)".>

Diagnose a failure with runtime evidence before changing code:

```bash
aspire describe --apphost <csproj>                  # state, health, exit codes
aspire logs <resource> --apphost <csproj> --tail 100
aspire otel traces <resource> --apphost <csproj> [--has-error true]
aspire otel logs <resource> --apphost <csproj> --severity Warning
aspire resource <resource> restart --apphost <csproj>
```

## Troubleshooting

| Symptom | Check |
|---|---|
| Container resources don't start | Docker/Podman running? `aspire doctor` |
| A service waits forever | `aspire logs <service>` shows which dependency it waits for; check that resource's logs/exit code |
| Migrations failed | `aspire logs <migrations resource>` |
| Service shows Unhealthy | `/health` exists only in Development; `aspire logs <service>` |
| Database login fails after re-setup | The user-secrets password no longer matches the volume. See Reset local data |
| HTTPS/certificate errors | `aspire certs trust` (or `dotnet dev-certs https --trust`) |
| Port already in use | Another instance running? `aspire ps`; use `aspire start --isolated` for a parallel copy |
