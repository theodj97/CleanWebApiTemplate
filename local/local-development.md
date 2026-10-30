# Local development

This repository runs locally with [Aspire](https://aspire.dev). One command starts the API, an ephemeral
PostgreSQL database built from SQL scripts (plus a synthetic seed), an ephemeral OpenBao server that holds the
application's secrets, and the dashboard with logs and traces.

Nothing here is required by deployed environments: `src/` never references anything under `local/`.

## Repository layout

| Folder | Contents |
|---|---|
| `src/` | Application and shared runtime code for every environment: `Api`, `Application`, `Domain`, `Infrastructure`, `ServiceDefaults`. |
| `tests/` | Test projects for `src/`: Api.UnitTests, Api.ComponentTests, Api.FunctionalTests, Tests.Shared. |
| `local/aspire/` | Local orchestration only: `CleanWebApiTemplate.AppHost`, `CleanWebApiTemplate.DbInitializer` and `CleanWebApiTemplate.OpenBaoInitializer`. |
| `local/openapi/` | Local-only Swagger sidecar, the examples catalog (`examples.json`, `bodies/`, `responses/`), the dropdown users (`users.json`), the custom UI (`swagger/`) and its unit tests. |
| `local/database/` | PostgreSQL schema scripts and `seed/` synthetic data. |
| `local/tools/` | `devtoken.cs` / `devtoken.json`: locally signed JWTs for scripts and `curl` (the Swagger profile picker uses `local/openapi/users.json`). |

`src/CleanWebApiTemplate.ServiceDefaults` is shared runtime code: the API calls `AddServiceDefaults()` in every
environment for OpenTelemetry and the base health checks (`/health`, `/alive`). That is why it lives in `src/`: the
telemetry and the probes are needed in deployed environments too, not only locally.

`src/CleanWebApiTemplate.Infrastructure` also holds the OpenBao configuration adapter (`Infrastructure/Secrets`),
shared by the API and the local `openbao-init` job. It stays in `src/` because the API reads it in every environment.

## Prerequisites

| Tool | Version used | Check |
|---|---|---|
| .NET SDK | 10.0.1xx (`global.json`: 10.0.100 with `rollForward: latestFeature`) | `dotnet --version` |
| Aspire CLI | 13.6.4 | `aspire --version`. Install: <https://aspire.dev/get-started/install-cli/> |
| Container runtime | Docker Engine/Desktop 29.x (or Podman with `ASPIRE_CONTAINER_RUNTIME=podman`) | `docker info` |
| HTTPS development certificate | trusted for `localhost` | `dotnet dev-certs https --check --trust` |

The API serves HTTPS only, with the ASP.NET development certificate. Create and trust it once per machine:

```bash
dotnet dev-certs https --trust
# Linux: .NET exports the certificate instead of installing it in the system store.
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
```

Export `SSL_CERT_DIR` in the shell that runs `aspire start`, so the AppHost, its health checks, the `swagger` sidecar
and `curl` trust the certificate.

## Topology

```text
postgres (container, ephemeral, no volume)
  └── postgresdb ──► db-init (runs once, applies schema + seed) ──► api (https://localhost:5001)
openbao (container, ephemeral, dev mode, no volume)
  └── openbao-init (runs once, writes the application's secrets) ──► api: OpenBao__* (reads API_KEY from the store)
local-jwt-signing-key (secret parameter) ──► openbao-init: API_KEY ──► openbao
api ──► swagger (Local only: UI + examples catalog, proxies /api/* to api)
```

| Resource | Kind | Purpose | Waits for |
|---|---|---|---|
| `postgres` | Container | PostgreSQL server. Ephemeral: removed with its data when the AppHost stops. | — |
| `postgresdb` | Database | Database referenced as `ConnectionStrings:Postgres`. | `postgres` |
| `db-init` | Project (one-shot) | Applies `local/database/*.sql` and, in Local mode, `local/database/seed/*.sql`; exits 0 when ready. | `postgresdb` |
| `openbao` | Container | OpenBao in dev mode: in-memory, unsealed, no volume. Removed when the AppHost stops. | — |
| `openbao-init` | Project (one-shot) | Writes the application's secrets (today `API_KEY`) into `secret/cleanwebapitemplate/local`; exits 0 when the store is ready. | `openbao` |
| `local-jwt-signing-key` | Secret parameter | HS256 key stored in the AppHost user secrets; passed to `openbao-init` as `API_KEY`. | — |
| `api` | Project | The API in the `Development` environment. | `postgresdb`, `db-init`, `openbao`, `openbao-init` completion |
| `swagger` | Project | Local-only Swagger UI with the examples in `local/openapi/`; proxies `/api/*` to `api`. Not started by the tests. | `api` |

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
  - `GET /openapi/v1.json` — OpenAPI document (the sidecar consumes it)
  - `GET /health` (aggregate status of all checks), `GET /alive`, `GET /health/api`, `GET /health/postgresDb`
- **Swagger with local examples and login:** the `swagger` resource in the dashboard (see § Swagger with local examples and login).

## Authentication: locally signed JWTs

The API validates JWTs with the symmetric key it reads from OpenBao as `API_KEY` and clears `Authentication:Authority`,
so **no identity
provider is contacted** in local development. The `swagger` sidecar mints the same HS256 tokens for its header
profile picker from `local/openapi/users.json`; the `devtoken` CLI remains available for scripts and `curl`.

```bash
dotnet local/tools/devtoken.cs list                          # profiles and roles
dotnet local/tools/devtoken.cs get --profile user            # token on stdout (scripts/curl)
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

The profile-picker users and the CLI profiles use the same emails and roles; the picker is defined in
`local/openapi/users.json`, the CLI in `local/tools/devtoken.json`.

The issuer (`https://localhost`) and audience (`https://localhost:5001`) come from `appsettings.json` (`ValidIssuers`
and `Audience`), and the AppHost passes them to the API and to the `swagger` sidecar; only the signing key changes per
machine. `devtoken init` stores it in the AppHost user secrets, outside the repository. Run `devtoken init --rotate` to
replace it (restart Aspire and select a profile again).

## Swagger with local examples and login (`local/openapi/`)

The `swagger` resource is a small sidecar that fetches the API's OpenAPI document, adds the request and response
examples from `local/openapi/examples.json` (`bodies/` and `responses/`), and serves its own Swagger UI. It proxies
`/api/*` to the API, so the browser only talks to one origin.

- Open the `swagger` resource in the dashboard; it is the only Swagger UI.
- The header profile picker lists the five users in `local/openapi/users.json`. Selecting a profile calls the sidecar's
  internal `POST /local/auth/token/{profile}` (not shown as an operation), mints the HS256 token and applies it with
  `ui.preauthorizeApiKey`; the following "Try it out" calls carry the `Authorization` header. The "New token" button
  mints a new one.
- The UI follows the system colour scheme and has its own light/dark toggle. Nothing is persisted: no
  `persistAuthorization`, no cached document. The token lives in the page; after a refresh, select the profile again.
  The seed is rebuilt on every `aspire start`.
- `users.json` uses the same emails and roles as the seed: `user`, `operator` and `admin` own the seeded rows;
  `external` and `no-role` exercise 403.
- The sidecar gets the signing key and the issuer/audience/role claim from the AppHost; the signing key is the same
  text seeded into OpenBao as `API_KEY`. It is HS256-only and Local-only: the Component and Functional tests do not
  start it and it is not published.
- The fixed seed ids live in `examples.json` and `responses/`. Update them if
  `local/database/seed/0001__todos.sql` changes.
- Full catalog format and troubleshooting: `local/openapi/README.md`.

The API only exposes the OpenAPI document at `/openapi/v1.json`; the sidecar is the only UI.

## Database

- **Engine:** PostgreSQL, provisioned by Aspire in an ephemeral container (no volume, session lifetime).
- **Consumers:** the API through `ConnectionStrings:Postgres`.
- **Schema:** `local/database/*.sql`, applied in ordinal file-name order by `db-init` before the API starts.
  `0001__initial_schema.sql` creates the `Todo` table (`Id bytea` for the Ulid, texts, `timestamptz`, `integer`) and
  its indexes.
- **Seed:** `local/database/seed/*.sql`, applied after the schema and only in Local mode (four synthetic TODOs owned by
  `local.user@example.com`, `local.operator@example.com` twice and `local.admin@example.com`).
- **Persistence:** none. Data disappears with the container when the AppHost stops.
- **Title rules:** titles are trimmed and unique per owner (`CreatedBy`), enforced by a unique index; a duplicate
  returns `409 Conflict`. The `title` filter of `POST /api/Todo/filter` is a case-insensitive contains match
  (`ILIKE`), with `%`, `_` and `\` matched literally.

### Seed coverage

Every seeded row uses a `CreatedBy` value that matches the `email` in `local/openapi/users.json`. Right after
`aspire start` each Swagger profile can exercise the endpoints and get meaningful results:

| Swagger profile | Rol | Endpoint de ejemplo | Dato de la semilla | Swagger example |
|---|---|---|---|---|
| `user` | `User` | `GET /api/Todo/01HWXHRB80000G000000000001` | TODO 1, `CreatedBy = local.user@example.com` | `GET /api/Todo/{id}` (TODO 1) |
| `user` | `User` | `POST /api/Todo/filter` | TODO 1, solo los suyos | `POST /api/Todo/filter` (user) |
| `user` | `User` | `POST /api/Todo/` | Nueva fila; edita el título si repites | `POST /api/Todo` (new-todo) |
| `operator` | `Operator` | `DELETE /api/Todo/01HWXHRB80000G000000000004` | TODO 4, `CreatedBy = local.operator@example.com` | `DELETE /api/Todo/{id}` (TODO 4) |
| `operator` | `Operator` | `POST /api/Todo/filter { "status": [2, 4] }` | TODOs 2 y 4, solo los suyos | `POST /api/Todo/filter` (operator) |
| `admin` | `Admin` | `PUT /api/Todo/01HWXHRB80000G000000000003` | TODO 3, `CreatedBy = local.admin@example.com` | `PUT /api/Todo/{id}` (TODO 3) |
| `admin` | `Admin` | `POST /api/Todo/filter { "createdBy": ["local.user@example.com", "local.operator@example.com"] }` | TODOs 1, 2 y 4, de otros usuarios | `POST /api/Todo/filter` (admin) |
| `user` | `User` | `POST /api/Todo/titles` | Títulos paginados, solo los suyos | `POST /api/Todo/titles` (first-page) |
| `external` | `External` | — | Ningún endpoint expuesto usa la política `External` | — |
| `no-role` | — | `POST /api/Todo/` | Devuelve `403` (caso negativo esperado) | `POST /api/Todo` (no-role; expect 403) |

Only the `admin` profile can read or modify TODOs owned by another user. The `user` and `operator` profiles are
restricted to rows whose `CreatedBy` matches their token email, including `GET /api/Todo/{id}`,
`POST /api/Todo/titles`, `PUT /api/Todo/{id}` and `DELETE /api/Todo/{id}`.

The profile-picker users in `local/openapi/users.json` use these emails and roles; the examples catalog in
`local/openapi/examples.json` pre-fills the bodies, ids and response examples.

### Adding a schema or seed change

Add a new script, for example `local/database/0002__add_todo_due_date.sql` or
`local/database/seed/0002__more_todos.sql`. The next `aspire start` applies it inside a transaction.

## Configuration and secrets

The `appsettings*.json` files contain **no connection strings and no secrets**. Non-secret settings live in the
files; secrets come from OpenBao; locally, the AppHost points the API at both:

| Setting | Needed for | How to provide |
|---|---|---|
| `ConnectionStrings__Postgres` | Database location | Injected by the AppHost (`ConnectionStrings:Postgres`). Outside Aspire, set the environment variable. |
| `API_KEY` | JWT signing key | **Read from OpenBao** (`OpenBao:Path`). It never reaches the API as an environment variable: `openbao-init` seeds it from the secret parameter `local-jwt-signing-key` (`devtoken init`). |
| `OpenBao__Address` | Where the secrets store is | Injected by the AppHost (`http://localhost:<port>`). In a deployed environment, set it to the server URL. |
| `OpenBao__Token` | Token sent in `X-Vault-Token` | Generated by the AppHost on every start and never persisted. Deployed environments take it from the platform's secret mechanism. |
| `OpenBao__Mount` | KV v2 mount point | `secret`, injected by the AppHost. |
| `OpenBao__Path` | Secret that holds the configuration | `cleanwebapitemplate/local` locally; `cleanwebapitemplate/staging` and `.../production` are set per environment. |
| `OpenBao__TimeoutSeconds` | Timeout of the startup read | Optional, 5 by default. |
| `Authentication__Authority` | OIDC metadata address | Empty locally (no metadata download). When unset, no authority is configured and tokens are validated with `API_KEY` only. Metadata over plain HTTP is always refused (`RequireHttpsMetadata` keeps its default `true`). |
| `Audience` | JWT audience the API validates | `appsettings.json`; the AppHost overrides it per mode. |
| `Cors__AllowedOrigins__0…` | Origins allowed to call the API from a browser | Empty locally (no browser client). An empty list disables CORS; credentials require explicit origins. |
| `OpenApi__Enabled` | OpenAPI document and Swagger UI | `true` in `appsettings.json`, `false` in `appsettings.PRODUCTION.json`. |

Sources, in increasing precedence: `appsettings*.json` → user secrets → OpenBao → environment variables → command
line. Environment variables stay the emergency override, and the OpenBao source is a no-op when `OpenBao__Address`
is empty. The secret is read **once at startup**: rotating it means restarting the application.

The API reads these values and never branches on the environment name. What changes per environment lives in
`appsettings.{Environment}.json`, in environment variables, or in the OpenBao path.

### OpenBao

The local server runs in **dev mode**: in memory, unsealed, no volume. It is removed with the AppHost, so every start
begins empty and `openbao-init` writes the application's secrets again. The application reads them through the same
code path as in a deployed environment.

- **UI:** `aspire describe` prints the `openbao` URL. Open `<url>/ui/` and authenticate with method **Token** and the
  root token of this run.
- **Root token:** generated per start and never written to disk. The dashboard shows it in the `api` or
  `openbao-init` resource (Environment); `docker inspect` shows it in the container arguments.
- **Add or change a secret:** in the UI, open `secret/cleanwebapitemplate/local` and add the key exactly as the
  application reads it (`API_KEY`, `Some__Nested__Key`). Then restart the API with `aspire resource api restart`:
  the store is read once at startup.
- **Lost on restart:** anything added by hand disappears when the AppHost stops. `openbao-init` recreates `API_KEY`
  only.

In a deployed environment, point `OpenBao__Address`, `OpenBao__Token` and `OpenBao__Path` at the real server (and
`OpenBao__Mount` if the KV mount is not `secret`). Obtaining that token is the platform's job (AppRole, Kubernetes
auth, or a secret injected by the orchestrator); the application only needs it in configuration.


## Tests

```bash
dotnet test --solution CleanWebApiTemplate.slnx
```

`global.json` selects the Microsoft Testing Platform runner, so `dotnet test` requires `--solution` or `--project`.
The suite has unit, component and functional projects (see `tests/README.md`): component tests run the API in-process
against the AppHost's PostgreSQL and OpenBao containers, functional tests run it as a separate process. Both use the
same AppHost through `RunMode=Component|Functional`, with no dashboard, no telemetry, empty (schema-only) databases
and freshly seeded secrets.

The local Swagger sidecar's unit tests live under `local/openapi/CleanWebApiTemplate.OpenApiExamples.UnitTests`; they
test local-only code and are included by the solution command. `tests/` only contains tests of `src/`.

The test modes run under the `Testing` environment name, but there is no `appsettings.Testing.json`: everything the
tests need arrives as configuration, from the AppHost as environment variables (functional) or from the in-memory
settings of `ComponentAppFactory` (component). The test issuer is `urn:CleanWebApiTemplate:tests` and every run gets a
new signing key in memory; the AppHost seeds it into OpenBao and the API reads it from there, the same path as in
production. The component fixture also asserts that the key did not arrive as an environment variable, so a stray
`API_KEY` cannot make the suite pass without exercising the store. No identity provider is contacted.

## Diagnostics

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
| `'API_KEY' must be at least 32 UTF-8 bytes in Symmetric mode` | The API refuses to start with a short HS256 key. Rotate it with `dotnet local/tools/devtoken.cs init --rotate` and restart the AppHost. |
| `db-init` failed | `aspire logs db-init`: the failing script and PostgreSQL error are printed there. |
| `OpenBao initialization failed` | `aspire logs openbao-init`: check `OpenBao__Address`, the token and `OpenBao__Path`. |
| `'API_KEY' is required` | The secret did not reach the store: check `aspire logs openbao-init` and `OpenBao__Path`. |
| `OpenBao returned 403` | The token does not match the container's root token (a stale AppHost) or the policy denies the path. |
| `OpenBao returned 404` | The secret path does not exist: the initializer did not run or `OpenBao__Path` differs. |
| API `Unhealthy` | `/health` only returns the aggregate status (`Healthy`/`Unhealthy`), not the failing check. Query `/health/api` and `/health/postgresDb` (`curl https://localhost:5001/health/postgresDb`) and read `aspire logs api`. |
| Port 5001 already in use | Another API is running; stop it or change the launch profile. |
| 401 from the API | The token expired or was signed with a different key; select the profile again or press "New token"; run `devtoken init --rotate` and restart the AppHost if the key rotated. |
| `swagger` resource: `Could not read the OpenAPI document` | The API is not ready or `OpenApi:Enabled` is false; wait for the API and refresh. |
| `swagger` resource: `500` with problems | `local/openapi/examples.json` references an operation or parameter that the API document does not have; check `aspire logs swagger`. |
| `swagger` resource: TLS error | Same development certificate as the other clients: `dotnet dev-certs https --trust` and `SSL_CERT_DIR` in the shell that runs `aspire start`. |
| `swagger` resource: 401 after selecting a profile | The token expired or the key rotated; select the profile again or press "New token". |
| `swagger` resource: `Unknown profile` | `users.json` changed while the sidecar was running; restart the `swagger` resource or Aspire. |
| `swagger` resource: 403 | The selected profile lacks the policy's role; try `operator`, `admin` or `external`. |
| 403 from the API | The profile lacks the policy's role; try `operator`, `admin` or `external`. |
| TLS error / `SSL certificate problem` | The dev certificate is not trusted. Run `dotnet dev-certs https --trust` and export `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"` in the shell that runs `aspire start`. |
| No database spans in traces | Npgsql is not instrumented in this project, so only HTTP spans appear. |
