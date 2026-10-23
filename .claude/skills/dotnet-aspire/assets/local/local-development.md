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
| HTTPS dev certificate | trusted | `dotnet dev-certs https --check --trust` (exit 0); see "First time on this machine" |

## First time on this machine

```bash
aspire certs trust                      # trusts the HTTPS development certificate (or: dotnet dev-certs https --trust)
dotnet local/tools/devtoken.cs init     # creates the local JWT signing key in the AppHost user secrets (outside the repo)
```

<Platform notes that apply: Windows confirmation dialog, macOS keychain password, Linux SSL_CERT_DIR, WSL: also trust
on the Windows side.>

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
  contain synthetic data only and are reinstalled on every start. The tests use the same initializer without the seed.

### Seed coverage

Every endpoint works right after start with these profiles and seeded rows (verified with the seed coverage check):

| Endpoint | devtoken profile | Seeded data (ids) | Expected |
|---|---|---|---|
| <GET /orders> | <orders-customer> | <2 orders of local.customer: 5e0d…0001, 5e0d…0002> | <200, 2 items> |
| <DELETE /orders/{id}> | <orders-admin> | <order 5e0d…0001> | <204> |
| <GET /orders> | <orders-no-roles> | — | <403> |
- The initializer's output is in the dashboard or `aspire logs db-init`.

### Adding seed data

Add `local/database/seed/NNN_description.sql` (next free number) written for an empty database with the current
schema, add its rows to the seed coverage table, then restart the AppHost. A new endpoint, migration or policy updates
the seed and the table in the same change. A broken script stops the initializer and the APIs keep waiting: check
`aspire logs db-init`.

### Adding a migration

<e.g. `dotnet ef migrations add <Name> -p src/<migrations project> -s <startup project>`>

## Configuration and external services

| Setting | Needed for | How to provide |
|---|---|---|
| <Parameters:x / external API key> | <feature> | `aspire secret set Parameters:<x> <value> --apphost …` or env `Parameters__<x>` |

<Services that stay external and what happens without them. Identity providers are never needed locally.>

## Bruno collection

<Only with a brunoGen layer in tools/bruno.> The `<aspire-local>` environment targets this AppHost and the seeded data;
tokens come from devtoken, never from the login endpoint.

```bash
dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env     # DEVTOKEN_<PROFILE> keys, git-ignored (re-run when they expire)
brunoGen import                                                   # regenerate the collection (edit only tools/bruno/custom)
cd <Solution>Collection && npx -y @usebruno/cli run --env <aspire-local>
```

Requests that change data (<delete, ship…>) consume their seeded rows: restart the AppHost to reinstall the seed.

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
| HTTPS/certificate errors | `dotnet dev-certs https --check --trust`; run `aspire certs trust` (WSL: also on Windows) |
| An endpoint returns an empty list or 404 locally | The seed lacks data for it: add rows and a coverage-table entry |
| Port already in use | Another instance running? `aspire ps`; use `aspire start --isolated` for a parallel copy |
