# Sources

Use official documentation for API details and verify versions against the generated repository's
`Directory.Packages.props` and `global.json`. Do not invent members from a different version.

## Official documentation

| Topic | Link |
|---|---|
| .NET 10 | <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10> |
| Native AOT deployment | <https://learn.microsoft.com/dotnet/core/deploying/native-aot/> |
| Native AOT limitations | <https://learn.microsoft.com/dotnet/core/deploying/native-aot/limitations> |
| Trimming warnings | <https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-warnings> |
| Minimal APIs | <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis> |
| Route groups | <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers#route-groups> |
| OpenAPI in ASP.NET Core | <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi> |
| JWT bearer authentication | <https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication> |
| Policy-based authorization | <https://learn.microsoft.com/aspnet/core/security/authorization/policies> |
| ProblemDetails (RFC 9457) | <https://www.rfc-editor.org/rfc/rfc9457> |
| System.Text.Json source generation | <https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation> |
| Configuration in .NET | <https://learn.microsoft.com/dotnet/core/extensions/configuration> |
| Options validation | <https://learn.microsoft.com/dotnet/core/extensions/options-validation> |
| Npgsql | <https://www.npgsql.org/doc/> |
| FluentValidation | <https://docs.fluentvalidation.net/> |
| ArchUnitNET | <https://github.com/TNG/ArchUnitNET> |
| xUnit v3 | <https://xunit.net/docs/getting-started/v3/getting-started> |
| Microsoft Testing Platform | <https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-microsoft-testing-platform> |
| Respawn | <https://github.com/jbogard/Respawn> |
| Aspire | <https://aspire.dev/> |
| Aspire testing | <https://learn.microsoft.com/dotnet/aspire/testing/overview> |
| OpenBao | <https://openbao.org/docs/> |
| OpenBao KV v2 | <https://openbao.org/docs/secrets/kv/kv-v2/> |
| ULID | <https://github.com/Cysharp/Ulid> |
| Conventional Commits | <https://www.conventionalcommits.org/> |

## Versions the template targets

Read the actual files before quoting a version:

- `global.json`: .NET SDK 10.0.100, `latestFeature`, Microsoft Testing Platform runner.
- `Directory.Packages.props`: central package versions (ASP.NET Core 10, Aspire 13.x, Npgsql 10.x,
  FluentValidation 12.x, xUnit v3, ArchUnitNET, Respawn, Bogus, NSubstitute, OpenTelemetry).
- `Directory.Build.props`: `net10.0`, nullable, implicit usings, warnings as errors, code style in build,
  NuGet audit.
- `src/{App}.Api/{App}.Api.csproj`: `PublishAot`, `IsAotCompatible`, `EnableAotAnalyzer`,
  `EnableRequestDelegateGenerator`, XML docs, banned API analyzer.

When a command or API differs between versions, trust the installed SDK/tool (`dotnet --info`,
`aspire --version`, `dotnet format --help`) and the repository's own configuration over a web result.

## Local-only references

- `README.template.md` (the generated project README) and `README.md` (the template repository) — commands,
  architecture, API and configuration.
- `tests/README.md` — test projects, identities, conventions, coverage.
- `local/local-development.md` — Aspire topology, OpenBao, local JWTs, Swagger sidecar.
- `local/openapi/README.md` — sidecar, examples catalog and profile picker.
- `AGENTS.md` — Native AOT and hook rules.
- `docs/audit/` — the template repository's audit history; **not shipped** with generated projects, so never
  point a generated project at it.
