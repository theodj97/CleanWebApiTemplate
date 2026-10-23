<!--
Template for local/local-development.md. Replace every <placeholder> with verified values, delete sections that do
not apply, and never write tokens, keys, dashboard login links, generated passwords or guessed ports.
-->
# Local development

This repository runs locally with [Aspire](https://aspire.dev). One command starts the applications and their
infrastructure; the Aspire dashboard shows resources, logs, traces and metrics. The tests start the **same AppHost**
in test modes, so local runs and tests share one topology.

## Repository layout

| Folder | Contents |
|---|---|
| `src/` | Application and shared runtime code, deployed to every environment: <projects> |
| `tests/` | Automated tests: <projects>. They start the AppHost below in `Component`/`Functional` mode. |
| `local/` | Local only: the AppHost (`local/aspire/{App}.AppHost`)<, database initializer, seed scripts, devtoken tool, scripts>. Nothing in `src/` depends on it. |

<If a ServiceDefaults project exists:> `src/{App}.ServiceDefaults` is shared runtime code: services call
`AddServiceDefaults()` in every environment for OpenTelemetry, health checks, service discovery and HTTP resilience.

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | <from global.json / verified> | `dotnet --version` |
| Aspire CLI | <verified version> or later | `aspire --version`. Install: <https://aspire.dev/get-started/install-cli/> |
| Container runtime | Docker <version> or Podman (`ASPIRE_CONTAINER_RUNTIME=podman`) | `docker info` |
| HTTPS dev certificate (optional) | — | `dotnet dev-certs https --check --trust`; trust it with `aspire certs trust` or `dotnet dev-certs https --trust` |

## First time on this machine

```bash
dotnet local/tools/devtoken.cs init     # creates the local JWT signing key in the AppHost user secrets (outside the repo)
```

## Topology

```text
<resource graph, e.g.
postgres (container) ──► ordersdb ──► db-init (schema + seed, then exits) ──► orders-api
                                                                              catalog-api
>
```

| Resource | Kind | Purpose | Depends on (waits for) |
|---|---|---|---|
| <name> | <Project/Container/Database/Executable> | <purpose> | <deps> |

## Start and stop

From the repository root:

```bash
aspire run   --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj    # foreground, Ctrl+C to stop
aspire start --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj    # background
aspire stop  --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj    # stops it; databases start empty next time
```

Running the AppHost project from Visual Studio, Rider or `dotnet run --project local/aspire/{App}.AppHost` also works.

## Dashboard and endpoints

- **Dashboard:** open the login link printed by `aspire run`/`aspire start`, or get it again with
  `aspire ps --format Json` (`dashboardUrl`). The link contains a personal login token: don't share or commit it.
- **Application endpoints:** listed in the dashboard and in `aspire describe <resource>`. <Verified routes to try.>

## Tokens for protected endpoints

No identity provider is needed locally: every protected API trusts a local issuer and validates locally signed JWTs.

```bash
dotnet local/tools/devtoken.cs list                              # profiles: API, roles, scopes
dotnet local/tools/devtoken.cs get --profile <profile>           # prints only the token
```

| Profile | API | Roles / scopes | Use it to try |
|---|---|---|---|
| <profile> | <api> | <roles/scopes from the real policies> | <allowed endpoints / 403 cases> |

```bash
TOKEN=$(dotnet local/tools/devtoken.cs get --profile <profile>)
curl -H "Authorization: Bearer $TOKEN" <endpoint>/<route>
```

```powershell
$token = dotnet local/tools/devtoken.cs get --profile <profile>
Invoke-RestMethod <endpoint>/<route> -Headers @{ Authorization = "Bearer $token" }
```

Swagger/Scalar, Bruno or `.http` files: paste the token or read it from an environment variable; never commit it.
Profiles live in `local/tools/devtoken.json` (no secrets). `devtoken.cs init --rotate` replaces the key; restart the
AppHost afterwards. <If an API has its own login endpoint:> `POST <route>` still works locally with the seeded users
of `local/database/seed/` and signs with the same local key.

## Database

- Engine: <engine/version> (Aspire container `<resource>`), database `<name>`, consumed by <projects> through
  `ConnectionStrings:<key>`.
- **Every run starts from scratch.** Each `aspire run`/`aspire start` creates a new, empty database: nothing is kept
  between runs and there are no volumes to clean up.
- Schema: <strategy, e.g. "EF Core migrations in src/<…>/Migrations, applied by local/aspire/{App}.DbInitializer">.
- Seed: the scripts in `local/database/seed/` run in name order right after the schema, before the APIs start. They
  contain synthetic data only. The tests use the same initializer without the seed.
- The initializer's output is in the dashboard or `aspire logs db-init`.

### Adding seed data

Add `local/database/seed/NNN_description.sql` (next free number) written for an empty database with the current
schema, then restart the AppHost. A broken script stops the initializer and the APIs keep waiting: check
`aspire logs db-init`.

### Adding a migration

<e.g. `dotnet ef migrations add <Name> -p src/<migrations project> -s <startup project>`>

## Configuration and external services

| Setting | Needed for | How to provide |
|---|---|---|
| <Parameters:x / external API key> | <feature> | `aspire secret set Parameters:<x> <value> --apphost …` or env `Parameters__<x>` |

<Services that stay external and what happens without them. Identity providers are never needed locally.>

## Tests

```bash
dotnet test <solution>                       # unit tests need nothing; component/functional tests need Docker/Podman
```

See `tests/README.md`. The tests start their own containers on random ports with a new signing key per run, so they
can run while the local environment is up.

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
| AppHost stops at start: "local JWT signing key is missing" | Run `dotnet local/tools/devtoken.cs init` once |
| 401 with a devtoken token | Profile for another API? Key rotated without restarting the AppHost? |
| 403 with a devtoken token | Expected for profiles without the required role/scope (`devtoken.cs list`) |
| Container resources don't start | Docker/Podman running? `aspire doctor` |
| A service waits forever | `aspire logs <service>` shows which dependency it waits for; check that resource's logs/exit code |
| Schema or seed failed | `aspire logs db-init` (a seed script may not match the current schema) |
| Service shows Unhealthy | `/health` exists only in Development; `aspire logs <service>` |
| My data disappeared after a restart | Expected: every run starts from scratch. Put what you need in `local/database/seed/` |
| HTTPS/certificate errors | Use the HTTP endpoint, or trust the dev certificate (`aspire certs trust`) |
| Port already in use | Another instance running? `aspire ps`; use `aspire start --isolated` for a parallel copy |
