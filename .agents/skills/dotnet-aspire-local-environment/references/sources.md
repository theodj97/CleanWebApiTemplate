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
| Persisting data (volumes) | <https://aspire.dev/fundamentals/persist-data-volumes/> |
| Resource lifetimes | <https://aspire.dev/app-host/resource-lifetimes/> |
| External parameters / secrets | <https://aspire.dev/fundamentals/external-parameters/> |
| Testing | <https://aspire.dev/testing/overview/> |
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

## Behaviour verified on 2026-10-22

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
| `AddDatabase` created the missing PostgreSQL database | database.md |
| ServiceDefaults template maps `/health`/`/alive` only in Development and exports OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set | observability.md |
| A request produced a single server span; EF Core/Npgsql work appeared only as logs (no instrumentation) | observability.md |
| A 404 trace has `hasError: false` | observability.md |
| `aspire describe --format Json` includes resource environment values (generated DB password); `aspire start --format Json` returns `dashboardUrl` with `?t=` token | SKILL.md rule 7 |
| `aspire agent init --workspace-root . --skill-locations claudecode` installs into `./.claude/skills`; no `.mcp.json` without `--mcp` | agent-integration.md |
| `--skills all` adds `dotnet-inspect` and `playwright-cli`, and globally installs `@playwright/cli` via npm | agent-integration.md |
| `aspire stop --force --volumes` removed the Aspire-created volume `<apphost>-<hash>-postgres-data` without a prompt | database.md |
