# Local development

This repository runs locally with [Aspire](https://aspire.dev). One command starts the API, an ephemeral
PostgreSQL database built from SQL scripts (plus a synthetic seed) and the dashboard with logs and traces.

Nothing here is required by deployed environments: `src/` never references anything under `local/`.

## Repository layout

| Folder | Contents |
|---|---|
| `src/` | Application and shared runtime code for every environment: `Api`, `Application`, `Domain`, `Infrastructure`, `ServiceDefaults`. |
| `tests/` | Test projects: Api.UnitTests, Api.ComponentTests, Api.FunctionalTests, Tests.Shared. |
| `local/aspire/` | Local orchestration only: `CleanWebApiTemplate.AppHost` and `CleanWebApiTemplate.DbInitializer`. |
| `local/database/` | PostgreSQL schema scripts and `seed/` synthetic data. |
| `local/tools/` | `devtoken.cs` / `devtoken.json`: locally signed JWTs, no identity provider. |
| `tools/bruno/` | brunoGen layer (`custom/`) for the `aspire-local` Bruno collection. |

`src/CleanWebApiTemplate.ServiceDefaults` is shared runtime code: the API calls `AddServiceDefaults()` in every
environment for OpenTelemetry and the base health checks (`/health`, `/alive`). That is why it lives in `src/`: the
telemetry and the probes are needed in deployed environments too, not only locally.

## Prerequisites

| Tool | Version used | Check |
|---|---|---|
| .NET SDK | 10.0.113 (`global.json`) | `dotnet --version` |
| Aspire CLI | 13.6.1 | `aspire --version`. Install: <https://aspire.dev/get-started/install-cli/> |
| Container runtime | Docker 29.8.2 | `docker info` |
| HTTPS development certificate | trusted for `localhost` | `dotnet dev-certs https --check --trust` |

The API serves HTTPS only, with the ASP.NET development certificate. Create and trust it once per machine:

```bash
dotnet dev-certs https --trust
# Linux: .NET exports the certificate instead of installing it in the system store.
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
```

Export `SSL_CERT_DIR` in the shell that runs `aspire start`, so the AppHost, its health checks and `curl` trust the
certificate. Node (the Bruno CLI) ignores it: pass `NODE_EXTRA_CA_CERTS=~/.aspnet/dev-certs/trust/*.pem` instead.

## Topology

```text
postgres (container, ephemeral, no volume)
  └── postgresdb ──► db-init (runs once, applies schema + seed) ──► api (https://localhost:5001)
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
- **API:** `https://localhost:5001`
  - `GET /swagger` — OpenAPI UI (when `OpenApi:Enabled` is true, as in local development)
  - `GET /openapi/v1.json` — OpenAPI document
  - `GET /health` (all checks), `GET /alive`, `GET /health/api`, `GET /health/postgresDb`

## Authentication: locally signed JWTs

The API validates JWTs with the symmetric key in `API_KEY` and clears `Authentication:Authority`, so **no identity
provider is contacted** in local development. Tokens are signed locally by `local/tools/devtoken.cs`.

```bash
dotnet local/tools/devtoken.cs list                          # profiles and roles
dotnet local/tools/devtoken.cs get --profile user            # token on stdout
dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env # one DEVTOKEN_<PROFILE> per profile (Bruno)
TOKEN=$(dotnet local/tools/devtoken.cs get --profile operator)

curl -X POST https://localhost:5001/api/Todo/ \
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

The issuer (`https://localhost`) and audience (`https://localhost:5001`) come from `appsettings.json` (`ValidIssuers`
and `Audience`), and the AppHost passes them to the API; only the signing key changes per machine. `devtoken init`
stores it in the AppHost user secrets, outside the repository. Run `devtoken init --rotate` to replace it.

## Bruno collection (`tools/bruno/`)

`tools/bruno/` is a brunoGen layer: `bruno.config.json` plus a versioned `custom/` folder. `brunoGen import` downloads
the running API's OpenAPI and writes the generated collection to `CleanWebApiTemplateCollection/` (git-ignored).
**Edit only `custom/`**: `manifest.json`, `environments/`, `opencollection.yml`, `overrides/`, `scripts/` and
`.env.sample`. The next `import` overwrites the generated collection.

Prerequisites: `brunoGen` installed per user (`brunoGen --version`) and Node.js for the CLI (`npx -y @usebruno/cli`).

```bash
aspire start
dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env   # tokens for the 5 profiles (60 min by default)
brunoGen import && brunoGen check
cd CleanWebApiTemplateCollection && npx -y @usebruno/cli run --env aspire-local
```

- **Environment `aspire-local`:** `baseUrl = https://localhost:5001` (the `Development` launch profile). The seed ids
  live here as `seed…` variables (`seedUserTodoId`, `seedOperatorTodoId`, `seedAdminTodoId`,
  `seedDeletableTodoId`). The Bruno CLI is Node: export `NODE_EXTRA_CA_CERTS` pointing at the development certificate
  or it will reject the TLS connection.
- **Authentication:** collection auth is `bearer {{token}}`; the collection `before-request` script reads
  `DEVTOKEN_<PROFILE>` from `.env` and assigns it to `{{token}}`. Each request chooses its identity with the request
  variable `devtokenProfile` (`user`, `operator`, `admin`, `external`, `no-role`). No login endpoint is called.
- **`.env`:** git-ignored. `dotenv` upserts the five `DEVTOKEN_*` lines, keeps other lines and writes the file with
  mode `600`. When tokens expire, run `dotenv` again and then `brunoGen check`/`import` (import copies `.env` into the
  generated collection).
- **Seed alignment:** every request uses seeded ids and bodies that pass the validators; assertions check status,
  owner and seed values. `DELETE /api/Todo/{}` removes TODO 4, so it only passes once per AppHost start: run
  `aspire stop && aspire start` to restore the seed.
- **Opening in the Bruno app:** import the generated `CleanWebApiTemplateCollection/` folder and select the
  `aspire-local` environment. The same `custom/` layer is the source of truth; use `brunoGen export` to bring back
  changes made in the UI.

## Database

- **Engine:** PostgreSQL, provisioned by Aspire in an ephemeral container (no volume, session lifetime).
- **Consumers:** the API through `ConnectionStrings:Postgres`.
- **Schema:** `local/database/*.sql`, applied in ordinal file-name order by `db-init` before the API starts.
  `0001__initial_schema.sql` creates the `Todo` table (`Id bytea` for the Ulid, texts, `timestamptz`, `integer`) and
  its indexes.
- **Seed:** `local/database/seed/*.sql`, applied after the schema and only in Local mode (four synthetic TODOs, one per devtoken profile email).
- **Persistence:** none. Data disappears with the container when the AppHost stops.

### Seed coverage

Every seeded row uses a `CreatedBy` value that matches the `email` claim of a `devtoken` profile. Right after
`aspire start` each profile can exercise the endpoints and get meaningful results:

| devtoken profile | Rol | Endpoint de ejemplo | Dato de la semilla | Bruno request |
|---|---|---|---|---|
| `user` | `User` | `GET /api/Todo/01HWXHRB80000G000000000001` | TODO 1, `CreatedBy = local.user@example.com` | `GET /api/Todo/{}` |
| `user` | `User` | `POST /api/Todo/filter { "createdBy": ["local.user@example.com"] }` | TODO 1 | `POST /api/Todo/filter` |
| `user` | `User` | `POST /api/Todo/` | Nueva fila, título generado por el script | `POST /api/Todo/` |
| `operator` | `Operator` | `DELETE /api/Todo/01HWXHRB80000G000000000004` | TODO 4, `CreatedBy = local.operator@example.com` | `DELETE /api/Todo/{}` |
| `operator` | `Operator` | `POST /api/Todo/filter { "status": [2, 4] }` | TODOs 2 y 4 | `POST /api/Todo/filter` cambiando cuerpo y `devtokenProfile` |
| `admin` | `Admin` | `PUT /api/Todo/01HWXHRB80000G000000000003` | TODO 3, `CreatedBy = local.admin@example.com` | `PUT /api/Todo/{}` |
| `user` | `User` | `POST /api/Todo/titles` | Títulos paginados | `POST /api/Todo/titles` |
| `external` | `External` | — | Ningún endpoint expuesto usa la política `External` | — (403 en Todo) |
| `no-role` | — | `POST /api/Todo/` | Devuelve `403` (caso negativo esperado) | `POST /api/Todo/` con `devtokenProfile: no-role` |

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
| `Authentication__Authority` | OIDC metadata address | Empty locally (no metadata download). When unset, no authority is configured and tokens are validated with `API_KEY` only. Metadata over plain HTTP is always refused (`RequireHttpsMetadata` keeps its default `true`). |
| `Audience` | JWT audience the API validates | `appsettings.json`; the AppHost overrides it per mode. |
| `Cors__AllowedOrigins__0…` | Origins allowed to call the API from a browser | Empty locally (no browser client). An empty list disables CORS; credentials require explicit origins. |
| `OpenApi__Enabled` | OpenAPI document and Swagger UI | `true` in `appsettings.json`, `false` in `appsettings.PRODUCTION.json`. |

The API reads these values and never branches on the environment name. What changes per environment lives in
`appsettings.{Environment}.json` or in environment variables.

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

The test modes run under the `Testing` environment name, but there is no `appsettings.Testing.json`: everything the
tests need arrives as configuration, from the AppHost as environment variables (functional) or from the in-memory
settings of `ComponentAppFactory` (component). The test issuer is `urn:CleanWebApiTemplate:tests` and every run gets a
new signing key in memory; issuer and audience are passed to the API the same way. No identity provider is contacted.

## AI coding agents

The repository ships the `dotnet-aspire` skill in `.claude/skills/dotnet-aspire`, next to the official Aspire skills.
Refresh the official ones after upgrading the Aspire CLI:

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
| API `Unhealthy` | `curl --cacert "$HOME/.aspnet/dev-certs/trust" https://localhost:5001/health`; the health check reports the failing dependency. |
| Port 5001 already in use | Another API is running; stop it or change the launch profile. |
| 401 from the API | The token expired or was signed with a different key; run `devtoken init --rotate` and restart the AppHost. |
| 401 in Bruno | Tokens in `tools/bruno/custom/.env` expired (or the key was rotated): run `devtoken dotenv …` and `brunoGen import`; restart Aspire if you rotated the key. |
| 403 from the API | The profile lacks the policy's role; try `operator`, `admin` or `external`. |
| TLS error / `SSL certificate problem` | The dev certificate is not trusted. Run `dotnet dev-certs https --trust` and export `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"` in the shell that runs the client or `aspire start`. For Node (Bruno CLI) use `NODE_EXTRA_CA_CERTS`. |
| No database spans in traces | Npgsql is not instrumented in this project, so only HTTP spans appear. |
