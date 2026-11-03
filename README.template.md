# CleanWebApiTemplate

ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT, JWT authentication, PostgreSQL, OpenBao and
Aspire. Generated from the Clean Web API Template (`dotnet new clean-webapi`).

## Quickstart

Requirements:

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0.1xx (`global.json`: 10.0.100, `latestFeature`) | `dotnet --version` |
| Aspire CLI | 13.6.x | `aspire --version` (<https://aspire.dev/get-started/install-cli/>) |
| Docker Engine/Desktop | 29.x | `docker info` |
| HTTPS dev certificate | trusted for `localhost` | `dotnet dev-certs https --check --trust` |

First run, from the solution root:

```bash
git init                                   # Husky needs a Git repository
dotnet tool restore                        # Husky.Net, dotnet-coverage, ReportGenerator
dotnet husky install                       # pre-commit, commit-msg and pre-push hooks
dotnet dev-certs https --trust
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"   # Linux only
dotnet local/tools/devtoken.cs init        # local JWT signing key in the AppHost user secrets
aspire start                               # API + PostgreSQL + OpenBao + Swagger sidecar + dashboard
```

`aspire start` prints the dashboard link. The API listens on `https://localhost:5001`; open the `swagger` resource in
the dashboard for the local Swagger UI with examples and a profile picker. Full guide:
[`local/local-development.md`](local/local-development.md).

## Architecture

```text
src/
├── CleanWebApiTemplate.Api              Minimal API: routes, auth, ProblemDetails, OpenAPI (Native AOT)
├── CleanWebApiTemplate.Application      Use cases, validators, Result<T>, repository abstractions
├── CleanWebApiTemplate.Domain           Entities and domain constraints, no framework dependencies
├── CleanWebApiTemplate.Infrastructure   PostgreSQL repositories (Npgsql), OpenBao configuration source
└── CleanWebApiTemplate.ServiceDefaults  OpenTelemetry and base health checks
local/                                   Aspire AppHost, DB/OpenBao initializers, Swagger sidecar, devtoken
tests/                                   Unit, component, functional and architecture tests
```

Dependency rule: `Api → Application → Domain` and `Api → Infrastructure → Application`. `Application` never references
`Infrastructure`, and `Domain` references nothing. `tests/CleanWebApiTemplate.ArchitectureTests` enforces it, and
`BannedSymbols.txt` blocks reflection-based APIs.

### Native AOT

The API is published as Native AOT. No reflection, no `dynamic`, JSON only through the generated
`AppJsonSerializerContext`, and AOT/trimming warnings are fixed at their cause, never suppressed. See
[`AGENTS.md`](AGENTS.md). Before considering a change done:

```bash
dotnet build CleanWebApiTemplate.slnx                      # 0 warnings, 0 errors
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained                 # Native AOT, 0 warnings
```

## Configuration and secrets

`appsettings*.json` hold no secrets and no connection strings. Sources, in increasing precedence:
`appsettings*.json` → user secrets → OpenBao → environment variables → command line.

| Setting | Purpose |
|---|---|
| `ConnectionStrings__Postgres` | PostgreSQL connection string (injected by the AppHost locally). |
| `OpenBao__Address`, `OpenBao__Token`, `OpenBao__Mount`, `OpenBao__Path` | Secrets store read once at startup. Empty address disables the source. |
| `API_KEY` | HS256 signing key (Symmetric mode), read from OpenBao. At least 32 UTF-8 bytes; the API refuses to start otherwise. |
| `Authentication__Mode` | `Symmetric` (local/tests) or `Oidc` (deployed). Inferred from `Authentication__Authority` when empty. |
| `Authentication__Authority` | OIDC authority, absolute HTTPS URI. Must be empty in Symmetric mode. |
| `ValidIssuers`, `Audience` | Accepted JWT issuers and audience. |
| `Cors__AllowedOrigins__N`, `Cors__AllowCredentials` | Browser origins. An empty list disables CORS. |
| `OpenApi__Enabled` | Serves `/openapi/v1.json` (`false` in `appsettings.PRODUCTION.json`). |

Invalid authentication settings fail at startup with a clear message instead of returning 500 per request.

## API

All routes need a JWT. Policies are role based: `User` (User, Operator, Admin), `Operator` (Operator, Admin), `Admin`
(Admin) and `External` (External, Operator, Admin; no route uses it yet). A token without the `email` claim gets `403`.

| Method | Route | Policy | Success |
|---|---|---|---|
| `GET` | `/api/Todo/{id}` | User | `200`; `404` when missing or not owned |
| `POST` | `/api/Todo/filter` | User | `200`; `204` when empty |
| `POST` | `/api/Todo/titles` | User | `200`; `204` when empty |
| `POST` | `/api/Todo/` | User | `201` with `Location`; `409` for a duplicate title |
| `PUT` | `/api/Todo/{id}` | User | `200`; `404`, `400` or `409` |
| `DELETE` | `/api/Todo/{id}` | Operator | `200` with `true`, or `false` when no visible row matched |

Contract rules:

- Non-admin callers only see and change their own TODOs (`CreatedBy` = token `email`); admins see every owner.
- Titles are trimmed, 1–255 characters and **unique per owner** (database unique index); a duplicate returns `409`.
- The `title` filter is a case-insensitive **contains** match; `%`, `_` and `\` are matched literally.
- `status` must be 1 Pending, 2 InProgress, 3 Completed or 4 Cancelled; `0` or unknown values return `400`.
- Errors are RFC 9457 ProblemDetails with `traceId`, including `404`/`405` and malformed JSON (`400`).

Health: `/health` (aggregate status), `/alive`, `/health/api`, `/health/postgresDb`. The OpenAPI document is at
`/openapi/v1.json`.

## Tests

```bash
dotnet test --solution CleanWebApiTemplate.slnx                       # everything (Docker for component/functional)
dotnet test --project tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj   # no Docker
npm run coverage                                                       # coverage report + thresholds
```

`global.json` selects the Microsoft Testing Platform runner, so `dotnet test` needs `--solution` or `--project`.
Component and functional tests start their own containers through the AppHost; no external service is called. Details
in [`tests/README.md`](tests/README.md).

## Git hooks

| Hook | Runs |
|---|---|
| `pre-commit` | gitleaks on the staged changes and `dotnet format` on the staged `.cs` files. |
| `commit-msg` | Conventional subject: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`, `perf`, `revert`. |
| `pre-push` | Build, format, analyzers, every test and the vulnerable package audit (`npm run verify`). |

gitleaks is downloaded on the first commit (`scripts/install-gitleaks.sh`, pinned version and SHA-256). Skip the
hooks with `HUSKY=0 git ...` or `--no-verify`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet husky install` fails with `not a git repository` | Run `git init` first. |
| `The local JWT signing key is missing` | `dotnet local/tools/devtoken.cs init`, then start Aspire again. |
| `'API_KEY' must be at least 32 UTF-8 bytes` | Rotate the key: `dotnet local/tools/devtoken.cs init --rotate`. |
| TLS error / `SSL certificate problem` | `dotnet dev-certs https --trust` and export `SSL_CERT_DIR` (Linux). |
| Component/functional tests cannot reach Docker | Start Docker; for Podman set `ASPIRE_CONTAINER_RUNTIME=podman`. |
| `Specifying a project for 'dotnet test' should be via '--project'` | The MTP runner is active: use `--project`/`--solution`. |

More in [`local/local-development.md`](local/local-development.md#troubleshooting).
