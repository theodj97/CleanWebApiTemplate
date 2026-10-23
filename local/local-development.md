# Local development

This repository runs locally with [Aspire](https://aspire.dev). One command starts the API, an ephemeral
PostgreSQL database built from SQL scripts (plus a synthetic seed) and the dashboard with logs and traces.

Nothing here is required by deployed environments: `src/` never references anything under `local/`.

## Repository layout

| Folder | Contents |
|---|---|
| `src/` | Application and shared runtime code for every environment: `Api`, `Application`, `Domain`, `Infrastructure`, `ServiceDefaults`. |
| `tests/` | Test projects (currently empty). |
| `local/aspire/` | Local orchestration only: `CleanWebApiTemplate.AppHost` and `CleanWebApiTemplate.DbInitializer`. |
| `local/database/` | PostgreSQL schema scripts and `seed/` synthetic data. |
| `local/tools/` | `devtoken.cs` / `devtoken.json`: locally signed JWTs, no identity provider. |

`src/CleanWebApiTemplate.ServiceDefaults` is shared runtime code: services call `AddServiceDefaults()` in every
environment for OpenTelemetry, health checks, service discovery and HTTP resilience. That is why it lives in `src/`.

## Prerequisites

| Tool | Version used | Check |
|---|---|---|
| .NET SDK | 10.0.113 (`global.json`) | `dotnet --version` |
| Aspire CLI | 13.6.1 | `aspire --version`. Install: <https://aspire.dev/get-started/install-cli/> |
| Container runtime | Docker 29.8.2 | `docker info` |

The HTTPS development certificate is not needed: the AppHost uses a loopback HTTP profile and the API endpoint is
HTTP. Dashboard authentication stays enabled.

## Topology

```text
postgres (container, ephemeral, no volume)
  └── postgresdb ──► db-init (runs once, applies schema + seed) ──► api (http://localhost:5000)
local-jwt-signing-key (secret parameter) ──► api: API_KEY
```

| Resource | Kind | Purpose | Waits for |
|---|---|---|---|
| `postgres` | Container | PostgreSQL server. Ephemeral: removed with its data when the AppHost stops. | — |
| `postgresdb` | Database | Database referenced as `ConnectionStrings:Postgres`. | `postgres` |
| `db-init` | Project (one-shot) | Applies `local/database/*.sql` and, in Local mode, `local/database/seed/*.sql`; exits 0 when ready. | `postgresdb` |
| `local-jwt-signing-key` | Secret parameter | HS256 key stored in the AppHost user secrets; passed to the API as `API_KEY`. | — |
| `api` | Project | The API in the `Development` environment. | `postgresdb`, `db-init` completion |

## Start and stop

From the repository root (`aspire.config.json` points to the AppHost):

```bash
dotnet local/tools/devtoken.cs init   # once per machine: creates the local JWT key in the AppHost user secrets
aspire start                          # background, prints the dashboard link
aspire run                            # foreground, Ctrl+C to stop
aspire stop                           # stops the AppHost and removes its containers and data
```

Explicit AppHost path, if you prefer:

```bash
aspire start --apphost local/aspire/CleanWebApiTemplate.AppHost/CleanWebApiTemplate.AppHost.csproj
```

`dotnet run --project local/aspire/CleanWebApiTemplate.AppHost` also works given the Aspire CLI is installed
(`AspireUseCliBundle`).

The local database is ephemeral by design: every start rebuilds the schema and the seed, and stopping removes the
container and its data. There is nothing to reset by hand.

## Dashboard and endpoints

- **Dashboard:** open the link printed by `aspire start`/`aspire run`, or get it again with
  `aspire ps --format Json` (`dashboardUrl`). The link contains a personal login token: do not commit or share it.
- **API:** `http://localhost:5000`
  - `GET /swagger` — OpenAPI UI (`Development` only)
  - `GET /openapi/v1.json` — OpenAPI document
  - `GET /health` (all checks), `GET /alive`, `GET /health/api`, `GET /health/postgresDb`

## Authentication: locally signed JWTs

The API validates JWTs with the symmetric key in `API_KEY` and clears `Authentication:Authority`, so **no identity
provider is contacted** in local development. Tokens are signed locally by `local/tools/devtoken.cs`.

```bash
dotnet local/tools/devtoken.cs list                          # profiles and roles
dotnet local/tools/devtoken.cs get --profile user            # token on stdout
TOKEN=$(dotnet local/tools/devtoken.cs get --profile operator)

curl -X POST http://localhost:5000/api/Todo/ \
  -H "Authorization: Bearer $TOKEN" -H 'content-type: application/json' \
  -d '{"title":"Smoke test","description":"From local development"}'
```

| Profile | Role | Use |
|---|---|---|
| `user` | `User` | Create, read and update TODOs |
| `operator` | `Operator` | Same, plus `DELETE /api/Todo/{id}` |
| `admin` | `Admin` | Same as operator (policies include `Admin`) |
| `external` | `External` | External policy |
| `no-role` | — | Authenticated token without permissions: expect `403` |

The issuer (`http://localhost`) and audience (`http://localhost:7210`) are the values the application already pins
(`ValidIssuers` in `appsettings.json` and `Audience` in `ConfigureServices`); only the signing key changes per machine.
`devtoken init` stores it in the AppHost user secrets, outside the repository. Run `devtoken init --rotate` to replace
it.

## Database

- **Engine:** PostgreSQL, provisioned by Aspire in an ephemeral container (no volume, session lifetime).
- **Consumers:** the API through `ConnectionStrings:Postgres`.
- **Schema:** `local/database/*.sql`, applied in ordinal file-name order by `db-init` before the API starts.
  `0001__initial_schema.sql` creates the `Todo` table (`Id bytea` for the Ulid, texts, `timestamptz`, `integer`) and
  its indexes.
- **Seed:** `local/database/seed/*.sql`, applied after the schema and only in Local mode (three synthetic TODOs).
- **Persistence:** none. Data disappears with the container when the AppHost stops.

### Adding a schema or seed change

Add a new script, for example `local/database/0002__add_todo_due_date.sql` or
`local/database/seed/0002__more_todos.sql`. The next `aspire start` applies it inside a transaction.

## Configuration and secrets

The `appsettings*.json` files contain **no connection strings and no secrets**. Everything is supplied through
environment variables (Aspire does it locally):

| Setting | Needed for | How to provide |
|---|---|---|
| `ConnectionStrings__Postgres` | Database location | Injected by the AppHost (`ConnectionStrings:Postgres`). Outside Aspire, set the environment variable. |
| `API_KEY` | JWT signing key | Injected by the AppHost from the secret parameter `local-jwt-signing-key` (`devtoken init`). |
| `Authentication__Authority` | OIDC metadata address | Empty locally (no metadata download). When unset, the API keeps its previous default `http://localhost:7210`. |

In `Development`, environment variables take precedence over user secrets, so values injected by the AppHost win over
a developer's local configuration.

No external service is needed locally.

## Tests

```bash
dotnet test --solution CleanWebApiTemplate.slnx
```

`global.json` selects the Microsoft Testing Platform runner, so `dotnet test` requires `--solution` or `--project`.
The suite has unit, component and functional projects (see `tests/README.md`): component tests run the API in-process
against the AppHost's PostgreSQL container, functional tests run it as a separate process. Both use the same AppHost
through `RunMode=Component|Functional`, with no dashboard, no telemetry and empty (schema-only) databases.

The test modes use `ASPNETCORE_ENVIRONMENT=Testing`, so the API loads `appsettings.Testing.json`. The test issuer is
`urn:CleanWebApiTemplate:tests` and every run gets a new signing key in memory; the audience stays the value pinned in
`ConfigureServices` (`http://localhost:7210`). No identity provider is contacted.

## AI coding agents

The repository ships the `dotnet-aspire` skill in `.agents/skills`, next to the official Aspire skills. Refresh the
official ones after upgrading the Aspire CLI:

```bash
aspire agent init --non-interactive --workspace-root . \
  --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring \
  --skill-locations standard --mcp=false
```

MCP is not configured: agents use the Aspire CLI.

Diagnose failures with runtime evidence before changing code:

```bash
aspire describe                                   # state, health, exit codes
aspire logs api --tail 100
aspire logs db-init
aspire otel traces api --limit 20
aspire otel logs api --severity Warning
aspire resource api restart
```

## Troubleshooting

| Symptom | Check |
|---|---|
| `The local JWT signing key is missing` | Run `dotnet local/tools/devtoken.cs init` from the repository root, then start the AppHost again. |
| `ConnectionStrings.Postgres is required` | The environment variable `ConnectionStrings__Postgres` is missing; the AppHost injects it. |
| Every request returns 500 | `API_KEY` must be configured and long enough for HS256; check `aspire logs api`. |
| `db-init` failed | `aspire logs db-init`: the failing script and PostgreSQL error are printed there. |
| API `Unhealthy` | `curl http://localhost:5000/health`; the health check reports the failing dependency. |
| Port 5000 already in use | Another API is running; stop it or change the launch profile. |
| 401 from the API | The token expired or was signed with a different key; run `devtoken init --rotate` and restart the AppHost. |
| 403 from the API | The profile lacks the policy's role; try `operator`, `admin` or `external`. |
| No database spans in traces | Npgsql is not instrumented in this project, so only HTTP spans appear. |
