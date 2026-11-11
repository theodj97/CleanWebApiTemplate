# Sources and versions

Where the rules come from and the versions the template is tested against.

## Tested versions

| Component | Version |
|---|---|
| .NET SDK (`global.json`) | 10.0.100, `rollForward: latestFeature` |
| ASP.NET Core / `Microsoft.AspNetCore.OpenApi` / `JwtBearer` | 10.0.10 |
| `Microsoft.OpenApi` | 2.7.5 |
| Aspire AppHost SDK, `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Testing` | 13.6.5 |
| Npgsql | 10.0.3 |
| FluentValidation | 12.1.1 |
| Ulid | 1.4.1 |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 |
| `Microsoft.IdentityModel.JsonWebTokens` | 8.23.0 |
| OpenTelemetry (exporter, hosting, AspNetCore, Http, Runtime) | 1.20.1 |
| xUnit v3 (`mtp-v2`, `assert`, `extensibility.core`) | 3.2.2 |
| ArchUnitNET (+ xUnit v3) | 0.13.5 |
| Respawn | 7.0.0 |
| Bogus | 35.6.5 |
| NSubstitute (+ analyzer) | 6.2.0 / 1.0.17 |
| BannedApiAnalyzers | 5.6.0 |
| Swashbuckle.AspNetCore.SwaggerUI (sidecar only) | 10.2.3 |
| Husky.Net | 0.9.1 |
| dotnet-coverage | 18.12.0 |
| ReportGenerator | 5.5.11 |
| gitleaks | 8.30.1 |
| OpenBao image | `openbao/openbao:2.7.2` |

Versions live in `Directory.Packages.props` (central package management), `global.json`,
`.config/dotnet-tools.json` and `local/aspire/.../OpenBao.cs`. Change them there, never in a project file.

## Official documentation

- ASP.NET Core Minimal APIs: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis>
- Native AOT deployment: <https://learn.microsoft.com/dotnet/core/deploying/native-aot/>
- Trimming and AOT warnings: <https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings>
- System.Text.Json source generation: <https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation>
- Microsoft.AspNetCore.OpenApi: <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi>
- OpenAPI in .NET 10: <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview>
- JWT bearer authentication: <https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication>
- Authorization policies: <https://learn.microsoft.com/aspnet/core/security/authorization/policies>
- Options pattern and validation: <https://learn.microsoft.com/dotnet/core/extensions/options>
- Npgsql: <https://www.npgsql.org/doc/>
- NpgsqlDataSource: <https://www.npgsql.org/doc/basic-usage.html#data-source>
- FluentValidation: <https://docs.fluentvalidation.net/>
- Ulid: <https://github.com/Cysharp/Ulid>
- Aspire: <https://aspire.dev/>
- Aspire testing: <https://learn.microsoft.com/dotnet/aspire/testing/overview>
- OpenBao: <https://openbao.org/docs/>
- Vault KV v2: <https://developer.hashicorp.com/vault/docs/secrets/kv/kv-v2>
- RFC 9457 Problem Details: <https://www.rfc-editor.org/rfc/rfc9457>
- Respawn: <https://github.com/jbogard/Respawn>
- xUnit v3: <https://xunit.net/docs/getting-started/v3/getting-started>
- ArchUnitNET: <https://github.com/TNG/ArchUnitNET>
- Conventional Commits: <https://www.conventionalcommits.org/>
- Keep a Changelog: <https://keepachangelog.com/>
- Semantic Versioning: <https://semver.org/>

## Repository documents

| Document | Content |
|---|---|
| `README.md` | Quickstart, configuration, API table, tests, hooks. |
| `AGENTS.md` | The non-negotiable rules for coding agents. |
| `GLOSSARY.md` | Domain language. |
| `docs/adr/` | Decisions that are hard to reverse. |
| `local/local-development.md` | Aspire topology, seeds, devtoken, troubleshooting. |
| `local/openapi/README.md` | Sidecar, catalog and profile picker. |
| `tests/README.md` | Test projects, identities and conventions. |
| `CHANGELOG.md` | Version history. |

The repository documents win over this skill when they differ. If a rule here is wrong, fix the skill in the
same change that fixes the code.
