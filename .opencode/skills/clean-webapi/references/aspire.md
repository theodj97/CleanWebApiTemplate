# Aspire local environment

One AppHost serves local development and the test suites. This reference covers its topology, the modes, the
one-shot jobs and the CLI commands an agent needs.

## Files

| File | Role |
|---|---|
| `aspire.config.json` | Points the Aspire CLI at the AppHost project. |
| `local/aspire/CleanWebApiTemplate.AppHost/AppHost.cs` | Topology and modes. |
| `…/AppHostContract.cs` | Run modes, resource names, configuration keys and JWT trust. Compiled with `APPHOST` here; linked into `Tests.Shared` and the Swagger sidecar without it. |
| `…/OpenBao.cs` | OpenBao resource and the initializer wiring. |
| `…/CleanWebApiTemplate.DbInitializer` | Applies `local/database/*.sql` and, in Local mode, the seed. |
| `…/CleanWebApiTemplate.OpenBaoInitializer` | Writes the JWT key and the connection string into OpenBao. |
| `local/openapi/CleanWebApiTemplate.OpenApiExamples` | Swagger sidecar (Local only). |
| `local/tools/devtoken.cs` | Local JWT CLI. |

The AppHost SDK is `Aspire.AppHost.Sdk/13.6.5`; the hosting and testing packages are pinned in
`Directory.Packages.props`.

## Modes

`RunMode` selects the mode; absent means `Local`.

| Mode | Starts | API | Seed | Telemetry | Sidecar |
|---|---|---|---|---|---|
| `Local` | everything | `Development` profile, HTTPS on 5001, health check | yes | dashboard | yes |
| `Component` | infrastructure + schema only | in-process `WebApplicationFactory` | no | off | no |
| `Functional` | infrastructure + schema + API | separate process, HTTP, `Testing`, explicit start | no | off | no |

Containers are ephemeral in every mode: no volumes, no fixed lifetimes. Every start begins with empty
containers, rebuilds the schema and (Local only) the seed, and reseeds the secrets.

## Resources

| Name | Kind | Notes |
|---|---|---|
| `postgres` | container | PostgreSQL; `postgresdb` is the database resource. |
| `db-init` | one-shot project | `Schema__Path=local/database`; `Seed__Path=local/database/seed` in Local only; exits 0 when the schema has at least one table. |
| `openbao` | container | `openbao/openbao:2.7.2`, dev mode (in memory, unsealed), health `/v1/sys/health`. |
| `openbao-init` | one-shot project | Receives `OpenBao:*`, `API_KEY` and `ConnectionStrings__Postgres`; writes both secrets; exits 0. |
| `api` | project | Local: Development profile + `/health`. Tests: HTTP endpoint, `Testing`, Warning logs, dead proxy, no telemetry, explicit start. |
| `swagger` | project | Local only; Swagger UI + examples catalog + profile picker; health `/health`. |

Ordering: `db-init` waits for `postgresdb`; `openbao-init` waits for `openbao` and `postgresdb`; the API waits
for `postgresdb`, `openbao` and both initializers. The sidecar waits for the API.

## Secrets

The API never receives a secret as an environment variable. It receives `OpenBao__Address`, `OpenBao__Token`,
`OpenBao__Mount` and `OpenBao__Path`; `openbao-init` writes `API_KEY` and `ConnectionStrings:Postgres` into
the store. The root token is random per start (or supplied through `Parameters:openbao-root-token`), so
nothing is persisted or committed.

The JWT trust (`ValidIssuers`, `Audience`, `Authority` cleared) is passed as configuration, not as a secret.
The signing key goes `Parameters:local-jwt-signing-key` → `openbao-init` → OpenBao → application.

## Local JWT

```bash
dotnet local/tools/devtoken.cs init [--rotate]     # once per machine; AppHost user secrets
dotnet local/tools/devtoken.cs list
dotnet local/tools/devtoken.cs get --profile admin
dotnet local/tools/devtoken.cs dotenv .env
```

Profiles live in `local/tools/devtoken.json` (`user`, `operator`, `admin`, `external`, `no-role`) with the
same emails as the seed and `devtoken.json`'s issuer/audience. The sidecar mints the same tokens from
`local/openapi/users.json`.

## CLI

```bash
aspire start                 # background; prints the dashboard URL
aspire ps --format Json      # resource states and dashboard URL
aspire describe api          # endpoints and environment
aspire logs db-init          # one-shot job output
aspire logs openbao-init
aspire resource api restart
aspire otel traces api       # telemetry
aspire stop                  # stops and removes containers and local data
```

The dashboard link contains a personal token: never commit it or paste it into a report.

## Adding a resource

- A new table or seed: nothing to change here; `db-init` applies every `*.sql` in ordinal order.
- A new secret: extend `OpenBao.cs` (`AddOpenBaoInitializer` and `WithOpenBaoSecrets`) and the initializer.
- A new application (another API or worker): add the project reference, the resource, its launch profile or
  test settings, and update `AppHostContract` plus `TestAuthSettings.Applications` and
  `FunctionalTestFixture.ApplicationResources` if it must be started in tests.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `The local JWT signing key is missing` | `dotnet local/tools/devtoken.cs init`, then start again. |
| `'API_KEY' must be at least 32 UTF-8 bytes` | Rotate the key: `devtoken init --rotate`. |
| `db-init` or `openbao-init` exits 1 | Read `aspire logs <resource>`; the message names the missing key or SQL error. |
| Docker not found | Start Docker; for Podman set `ASPIRE_CONTAINER_RUNTIME=podman`. |
| TLS error | `dotnet dev-certs https --trust`; on Linux export `SSL_CERT_DIR`. |
| Port 5001 busy | Stop the previous AppHost (`aspire stop`) or change the launch profile. |
