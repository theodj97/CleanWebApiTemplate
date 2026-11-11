# Aspire

The template has **one AppHost** under `local/aspire/{App}.AppHost`. It serves local development and the
component/functional test fixtures. There is no second AppHost for tests.

For setup, repair or deep debugging of the environment (no AppHost, a duplicated test AppHost, a broken
container, a missing `devtoken`), use the **`aspire`** skill; for the start/stop lifecycle use
`aspire-orchestration`. This reference covers the template's own topology and the points a feature change
touches.

## Topology

```text
postgres (container, ephemeral, no volume)
  └── postgresdb ──► db-init (schema + Local seed) ──► api
openbao (container, dev mode, in memory)
  └── openbao-init (writes API_KEY) ──► api: OpenBao__*
local-jwt-signing-key (secret parameter) ──► openbao-init: API_KEY ──► openbao
api ──► swagger (Local only: UI + examples, proxies /api/* to api)
```

| Resource | Kind | Purpose | Waits for |
|---|---|---|---|
| `postgres` | Container | PostgreSQL server; removed when the AppHost stops | — |
| `postgresdb` | Database | Database referenced as `ConnectionStrings:Postgres` | `postgres` |
| `db-init` | Project (one-shot) | Applies `local/database/*.sql` and, in Local mode, `local/database/seed/*.sql`; exits 0 when ready | `postgresdb` |
| `openbao` | Container | OpenBao dev mode: in-memory, unsealed, no volume | — |
| `openbao-init` | Project (one-shot) | Writes the application's secrets (`API_KEY`) into the store | `openbao` |
| `local-jwt-signing-key` | Secret parameter | HS256 key in the AppHost user secrets | — |
| `api` | Project | The API in `Development` | database/schema/secrets |
| `swagger` | Project | Local Swagger UI with examples; proxies `/api/*` | `api` |

The AppHost uses `AppHostContract` for run modes, resource names, configuration keys and the JWT trust
mapping. The file is compiled into `Tests.Shared` too, so the tests and the AppHost cannot drift on a name or a
key.

## Modes

`RunMode` selects the mode; absent means `Local`.

| Mode | Started by | Resources | Seed | API |
|---|---|---|---|---|
| `Local` | `aspire start` / `aspire run` | postgres, openbao, db-init, openbao-init, api, swagger | Schema + seed | Project, launch profile `Development`, HTTPS |
| `Component` | `ComponentTestFixture` via `DistributedApplicationTestingBuilder` | postgres, openbao, db-init, openbao-init | Schema only | Runs in-process (`WebApplicationFactory`) |
| `Functional` | `FunctionalTestFixture` | postgres, openbao, db-init, openbao-init, api | Schema only | Separate process, HTTP only, explicit start |

Test modes add:

- a per-run OpenBao root token (`Parameters:openbao-root-token`);
- a test issuer/audience/signing key as AppHost arguments;
- no telemetry (`OTEL_EXPORTER_OTLP_ENDPOINT` empty);
- no Swagger sidecar;
- no launch profile; HTTP only; dynamic ports;
- an outbound proxy (`HTTP_PROXY`/`HTTPS_PROXY`) that refuses external calls.

Component mode stops before the API resource: the test host runs it in-process. Functional mode declares the
API with `WithExplicitStart()` and the fixture starts it after the schema and secrets are ready.

## `db-init`

`local/aspire/{App}.DbInitializer/Program.cs`:

- reads `ConnectionStrings__Postgres`, `Schema__Path` and optional `Seed__Path`;
- applies every `*.sql` in ordinal order, each in a transaction;
- skips the seed when `Seed__Path` is unset (test modes);
- exits 1 with a message when the schema directory is missing/empty or a script fails;
- ends with a readiness query (`SELECT COUNT(1) FROM "Todo";` today). When the sample is removed or renamed,
  update this query to a table that still exists, or `db-init` exits 1 and the API waits forever.

A new table needs no AppHost change: drop the `000N__*.sql` file in `local/database` and the seed in
`local/database/seed`. A new database needs a new resource, a `WithReference` with the connection name the
application reads, and a resetter in the tests.

## `openbao-init`

`local/aspire/{App}.OpenBaoInitializer/Program.cs`:

- reads `OpenBao__Address`, `OpenBao__Token`, `OpenBao__Mount`, `OpenBao__Path` and the secret values;
- writes one KV v2 secret (`API_KEY` today) with `OpenBaoClient`;
- exits 0 when the write succeeds; non-zero leaves the API waiting;
- prints the key name and path, never the value.

The application receives `OpenBao:*` through `WithOpenBaoSecrets`; it reads the secret at startup. The AppHost
never passes `API_KEY` to the application as an environment variable. A new secret is added here, not in the
API's environment.

## Local JWT

- `devtoken init` writes `Parameters:local-jwt-signing-key` to the AppHost user secrets.
- `devtoken list` shows the profiles; `devtoken get --profile user` prints a token.
- `devtoken.json` defines the issuer, algorithm, per-API audience/role claim and profiles.
- `LocalJwt` reads the configuration, requires the key in Local mode, and passes the validation material to
  the API through `WithLocalJwtTrust` using `AppHostContract.JwtTrust`.
- The Swagger sidecar mints the same HS256 token from `local/openapi/users.json`.
- The API's `Authentication:Authority` is cleared in Local mode so no metadata is downloaded.

Never use a login endpoint to obtain local/test tokens. Never commit the key or a token.

## Swagger sidecar

`local/openapi/` is a small project that:

- fetches the API's `/openapi/v1.json`;
- merges the examples from `examples.json` (`bodies/`, `responses/`);
- serves its own Swagger UI with a profile picker (`users.json`);
- proxies `/api/*` to the API;
- is started only in Local mode.

The sidecar's `examples.json` keys are OpenAPI operation paths (`POST /api/Project/filter`, `POST /api/Project`).
A new endpoint without examples still appears; add examples when the feature has meaningful states.

Tests for the sidecar live under `local/openapi/{App}.OpenApiExamples.UnitTests`.

## Dashboard and commands

```bash
dotnet local/tools/devtoken.cs init       # once per machine
aspire start                              # background, prints the dashboard link
aspire run                                # foreground
aspire stop                               # stops and removes containers/data
aspire ps --format Json                   # resources + dashboardUrl (contains a login token: do not share)
aspire logs <resource>                    # logs of db-init, openbao-init, api
```

The dashboard link contains a personal login token. Do not commit it, paste it into a report or share it.

## Health, telemetry and CORS

- `/health` is the aggregate status; `/alive` is liveness; `/health/api` checks the API; `/health/postgresDb`
  checks PostgreSQL. They are mapped in every environment by `ServiceDefaults` and `ConfigureApplication`.
- `AddServiceDefaults` registers OpenTelemetry for ASP.NET Core, HTTP and runtime metrics. The OTLP exporter is
  enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; tests set it to an empty string, so they export
  nothing. Do not add a second telemetry pipeline in a feature.
- CORS is one policy (`DefaultCorsPolicy`) built from `Cors:AllowedOrigins` and `Cors:AllowCredentials`. An
  empty origin list allows nobody; `UseCors` runs after routing and before authentication. A new browser origin
  is configuration, not code.

## Adding a resource

1. Add the resource in `AppHost.cs` with a stable name in `AppHostContract.Resources`.
2. Add the configuration key(s) the application reads to `AppHostContract.AppSettings` (or a new nested class).
3. Pass the value with `WithEnvironment`/`WithReference` under the exact key the code reads
   (`ConnectionStrings:Postgres` → `ConnectionStrings__Postgres`).
4. Add `WaitFor`/`WaitForCompletion` so the application starts after its dependency is ready.
5. Add a local substitute, never a real external service; the functional tests refuse outbound calls.
6. Update the test fixture's `InfrastructureResources`, `SchemaInitializerResource`,
   `SecretsInitializerResource` or `StartApplicationAsync` if the resource affects startup.
7. Update `local/local-development.md` and `README.template.md`.
8. Run `aspire start` and prove the graph; run the component and functional tests.

## Feature points of contact

| Feature change | AppHost change |
|---|---|
| New table/column/index | None: add a SQL script; keep the `db-init` readiness query valid |
| New seed rows | None: add a seed script |
| New setting | Add the value/environment in `AppHost.cs` or the deployment config; add the key to `AppHostContract` |
| New secret | Add the secret to `openbao-init`; add the AppHost parameter/seed; do not pass it to the API |
| New role/profile | Update `devtoken.json` and `openapi/users.json`; no AppHost resource |
| New internal service (API, worker) | Add a project resource, trust mapping, tests, docs |
| New external dependency | Add a local substitute (WireMock/container/fake) in every mode; tests must record no outbound calls |

## Troubleshooting

| Symptom | Fix |
|---|---|
| `The local JWT signing key is missing` | `dotnet local/tools/devtoken.cs init`, then start again |
| `'API_KEY' must be at least 32 UTF-8 bytes` | `devtoken init --rotate` |
| `db-init` exits 1 | Read `aspire logs db-init`; a script failed, the directory is empty, or the readiness query references a removed table |
| `openbao-init` exits 1 | Read `aspire logs openbao-init`; a required `OpenBao__*` value or the secret is missing |
| TLS error / `SSL certificate problem` | `dotnet dev-certs https --trust`; on Linux export `SSL_CERT_DIR` |
| Component/functional tests cannot reach Docker | Start Docker; for Podman set `ASPIRE_CONTAINER_RUNTIME=podman` |
| Tests fail with `CS0433: The type 'Program' exists in both` | Reference the AppHost with `Aliases="apphost"` and use `extern alias apphost;` |
| `Blocked outbound HTTP call to ...` | Point the dependency to a local substitute; do not relax the handler |
| `JwtBearer scheme '…' would download metadata` | Clear `Authority`/`MetadataAddress` through `JwtTrust` (`TrustValue.Empty`) |
| Swagger profile picker returns 401 | The sidecar and API disagree on issuer/audience/key; check `devtoken.json` and the AppHost environment |
