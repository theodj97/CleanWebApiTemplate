# Sources and version notes

Prefer these maintained sources over anything restated in this skill. When a page has moved, follow the redirect
(Aspire docs have moved from learn.microsoft.com to aspire.dev). The installed CLI can also search and read aspire.dev
from the terminal: `aspire docs search <terms>`, `aspire docs get <slug>`, `aspire docs api search <member>`,
`aspire docs api get <id>`.

## Authoritative documentation

| Topic | Link |
|---|---|
| Aspire overview | <https://aspire.dev> (formerly <https://learn.microsoft.com/dotnet/aspire>) |
| AppHost | <https://aspire.dev/get-started/app-host/> (formerly …/fundamentals/app-host-overview) |
| Aspire SDK / project shape | <https://aspire.dev/get-started/aspire-sdk/> |
| Add Aspire to an existing app | <https://aspire.dev/get-started/add-aspire-existing-app/> |
| Service defaults | <https://aspire.dev/get-started/csharp-service-defaults/> (formerly …/fundamentals/service-defaults) |
| Dashboard | <https://aspire.dev/dashboard/overview/> (formerly <https://learn.microsoft.com/dotnet/aspire/dashboard>) |
| Dashboard security | <https://aspire.dev/dashboard/security-considerations/> |
| Telemetry | <https://aspire.dev/fundamentals/telemetry/> |
| Integrations | <https://aspire.dev/integrations/overview/> (formerly …/components-overview) |
| EF Core migrations in Aspire | <https://aspire.dev/integrations/databases/efcore/migrations/> (formerly …/database/ef-core-migrations) |
| Resource lifetimes (session = ephemeral) | <https://aspire.dev/app-host/resource-lifetimes/> |
| External parameters / secrets | <https://aspire.dev/fundamentals/external-parameters/> |
| Testing | <https://aspire.dev/testing/overview/> |
| ASP.NET Core JwtBearer configuration (`Authentication:Schemes:{Scheme}`) | <https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication> |
| `dotnet user-jwts` (SDK tool, considered and not used, see below) | <https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn> |
| .NET file-based apps (`dotnet app.cs`) | <https://learn.microsoft.com/dotnet/core/sdk/file-based-apps> |
| User secrets location | <https://learn.microsoft.com/aspnet/core/security/app-secrets> |
| xUnit v3 | <https://xunit.net/docs/getting-started/v3/whats-new> |
| Respawn | <https://github.com/jbogard/Respawn> |
| WireMock.Net Aspire | <https://github.com/WireMock-Net/WireMock.Net/wiki/Using-WireMock.Net.Aspire> |
| AI coding agents | <https://aspire.dev/get-started/ai-coding-agents/> |
| Aspire agent skills | <https://aspire.dev/get-started/aspire-skills/> |
| AI-assisted debugging with the dashboard | <https://aspire.dev/dashboard/ai-coding-agents/> |
| Aspire MCP server | <https://aspire.dev/get-started/aspire-mcp-server/> |
| CLI reference | <https://aspire.dev/reference/cli/overview/>, commands under <https://aspire.dev/reference/cli/commands/> (`aspire-agent-init/`, `aspire-stop/`, `aspire-otel/`, …) |
| API reference (e.g. EF migrations hosting) | <https://aspire.dev/reference/api/csharp/aspire.hosting.entityframeworkcore/> |
| CLI install | <https://aspire.dev/get-started/install-cli/> |
| EF Core reverse engineering | <https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/> |
| `dotnet sln` | <https://learn.microsoft.com/dotnet/core/tools/dotnet-sln> |
| Claude Code skills | <https://code.claude.com/docs/en/skills> |

## Behaviour verified on 2026-10-22 (former local-environment skill)

Verified against .NET SDK 10.0.113, Aspire CLI 13.6.1, Aspire packages 13.6.3 and Docker 29.8, with a sample
repository (ASP.NET Core API + EF Core/PostgreSQL + `.slnx`). Re-verify on other versions.

| Finding | Where it matters |
|---|---|
| `dotnet new aspire-apphost -o local/aspire/X.AppHost` gives a project AppHost (`Aspire.AppHost.Sdk/13.6.3`, `AspireUseCliBundle`, `aspire.config.json`) | apphost.md |
| A hand-written AppHost csproj + `AppHost.cs` + launch profile builds and starts with `aspire start --apphost` | apphost.md |
| `aspire new aspire-empty` creates a file-based `apphost.cs` (no csproj) | apphost.md |
| `aspire init` puts `{App}.AppHost/` at the repo root, leaves the `.slnx` unchanged, installs `Aspire.ProjectTemplates` user-wide and tries to trust dev certs | apphost.md |
| `aspire add <name> --non-interactive` needs an exact id/friendly name, otherwise exit code 5 | apphost.md |
| `Aspire.Hosting.EntityFrameworkCore` (`AddEFMigrations`) exists only as preview (`13.6.3-preview.1.26521.2`). Attached to the API project, migrations ran, but the API lost `ASPNETCORE_URLS`/`ASPNETCORE_ENVIRONMENT`/`OTEL_EXPORTER_OTLP_ENDPOINT` | database.md |
| Migration worker under `local/aspire/` + `WaitForCompletion` initialized a fresh PostgreSQL database; API healthy, POST 201/GET 200 | database.md |
| Ephemeral PostgreSQL (no volume, session lifetime) + initializer running migrations then `local/database/seed/001_orders.sql`: run 1 showed the 2 seed rows, a POST added a third; after `aspire stop`/`aspire start` run 2 showed only the 2 seed rows. `describe` listed no volumes; the container and its anonymous volume were removed a few seconds after `aspire stop` (asynchronous clean-up) | database.md, validation-and-checklist.md |
| `AddDatabase` created the missing PostgreSQL database | database.md |
| ServiceDefaults template maps `/health`/`/alive` only in Development and exports OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set | observability.md |
| A request produced a single server span; EF Core/Npgsql work appeared only as logs (no instrumentation) | observability.md |
| A 404 trace has `hasError: false` | observability.md |
| `aspire describe --format Json` includes resource environment values (generated DB password); `aspire start --format Json` returns `dashboardUrl` with `?t=` token | SKILL.md rule 10 |
| `aspire agent init --workspace-root . --skill-locations claudecode` installs into `./.claude/skills`; no `.mcp.json` without `--mcp` | local-development.md |
| `--skills all` adds `dotnet-inspect` and `playwright-cli`, and globally installs `@playwright/cli` via npm | local-development.md |
| `aspire stop --force --volumes` removed the Aspire-created volume `<apphost>-<hash>-postgres-data` without a prompt | database.md |

## Behaviour verified on 2026-10-23 (AspireSkill)

Verified against .NET SDK 10.0.113, Aspire CLI 13.6.1, Aspire packages 13.6.3, Docker 29.8.2, EF Core 10.0.13,
Npgsql 10.0.3, Microsoft.IdentityModel 8.23.0, xunit.v3 4.0.2 (VSTest, `mtp-off`), Respawn 7.0.0, with a validation
repository (two APIs: one issuing and validating HS256 tokens with `Jwt:Key` in code and a login endpoint, one
validation-only with config-bound JwtBearer and a production `Authority`; PostgreSQL + EF Core migrations; `.slnx`,
CPM, `TreatWarningsAsErrors`). Re-verify on other versions.

| Finding | Where it matters |
|---|---|
| One AppHost with `RunMode` Local/Component/Functional served `aspire start` and both test types; 52 tests (5 unit, 29 component, 18 functional) passed twice in parallel projects in ~30 s, no containers left | apphost.md, integration-testing.md |
| Local mode and test runs worked at the same time (tests passed while the local AppHost served requests) | apphost.md |
| One `App.StartAsync` hang (5-minute timeout) in the first functional run; not reproduced in the 7 later runs of that project (alone, next to a running local AppHost, and in full parallel suites) | integration-testing.md |
| Test projects referencing the AppHost and an API both with top-level statements fail with `CS0433` (`Program` in both); `Aliases="apphost"` + `extern alias` fixes it | architecture-and-layout.md |
| `ExecuteCommandResult.ErrorMessage` is obsolete in 13.6 (`Message`): the former template failed with warnings as errors | integration-testing.md |
| `ParameterResource.GetValueAsync` inside `WithEnvironment(async context => …)` derives a base64 value from a secret parameter | apphost.md, local-jwt-authentication.md |
| An empty environment variable from the AppHost cleared a config-bound JwtBearer `Authority` (Linux) | local-jwt-authentication.md |
| With `Authority` NOT cleared and a dead proxy, every request still succeeded: the metadata download failure is not fatal and validation fell back to the explicit keys. The component guard and the functional `OutboundCallRecorder` (`CONNECT login.contoso.example:443`) detected it | local-jwt-authentication.md |
| Config-bound `SigningKeys:0:Value` (base64) + `ValidIssuer` + `ValidAudiences:0` validated HS256 tokens with roles in `roles` (`MapInboundClaims=false`) and scopes in `scp` | local-jwt-authentication.md |
| An API that validates with `SymmetricSecurityKey(UTF8(Jwt:Key))` accepted devtoken tokens and its own login tokens signed with the local key | local-jwt-authentication.md |
| `devtoken.cs` as a .NET 10 file-based app ran under a repository `Directory.Build.props` with `TreatWarningsAsErrors` and CPM, kept stdout clean (`$(…)` captures only the token), created no files in the repository, and wrote user secrets with modes 700/600 | local-jwt-authentication.md |
| RS256: `devtoken init` stored the private key (`LocalAuth:PrivateKeyPem`) and public key (`Parameters:local-jwt-public-key`) separately; a token validated with the public key only (token-level check, no API used RS256) | local-jwt-authentication.md |
| Expired (1 h past), not-yet-valid, wrong issuer, wrong audience, unknown key and unsigned (`alg: none`) tokens got 401 in-process and out of process; identities without roles/scopes got 403 | local-jwt-authentication.md |
| The HTTPS development certificate was untrusted on the machine; Local-mode health checks and HTTP endpoints still worked | local-development.md |
| `Respawn` 7.0.0 works with the `RespawnDatabaseResetter` template unchanged | database.md |
| EF Core consumer of a data library with `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets=all`) + Npgsql 10.0.3 failed with `CS1705` until EF Core was referenced explicitly | architecture-and-layout.md |

`dotnet user-jwts` was evaluated for local tokens and not adopted as the default: it signs only with a symmetric key
stored in the API project's user secrets and only reaches applications whose JwtBearer options are bound from
`Authentication:Schemes:{Scheme}`, while the inspected applications set `TokenValidationParameters` in code. It has no
profiles and couples keys to each API project instead of one local issuer.
