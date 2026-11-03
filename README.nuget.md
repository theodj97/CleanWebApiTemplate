# Clean Web API Template

ASP.NET Core 10 Minimal API with **Clean Architecture**, **Native AOT**, JWT authentication, PostgreSQL, OpenBao
and Aspire. Install it with `dotnet new` and generate a ready-to-run solution with tests, local infrastructure
and quality gates.

## Install

```bash
dotnet new install CleanWebApiTemplate
```

Pin a version:

```bash
dotnet new install CleanWebApiTemplate::1.1.0
```

Generate a project:

```bash
dotnet new clean-webapi -n MyCompany.Orders -o MyCompany.Orders
cd MyCompany.Orders
git init                                   # Husky needs a Git repository
dotnet tool restore
dotnet husky install
dotnet dev-certs https --trust
dotnet local/tools/devtoken.cs init        # local JWT signing key in the AppHost user secrets
aspire start                               # API + PostgreSQL + OpenBao + Swagger + dashboard
```

Uninstall the template:

```bash
dotnet new uninstall CleanWebApiTemplate
```

## Options

| Option | Effect |
|---|---|
| `-n`, `--name` | Replaces `CleanWebApiTemplate` in file names, namespaces and content. |
| `--no-restore` | Skips the NuGet and `dotnet tool restore` post-actions. |
| `--allow-scripts yes` | Runs `dotnet tool restore` without prompting (it is a script post-action). |

The generated project is self-contained: it does not include the template repository's audit, coverage reports,
agent skills or local tools.

## What you get

- Five runtime projects: `Api`, `Application`, `Domain`, `Infrastructure`, `ServiceDefaults`.
- Minimal API with route groups, role policies, owner scoping, ProblemDetails and source-generated OpenAPI/JSON.
- Native AOT: no reflection, no `dynamic`, source-generated JSON and request delegates.
- PostgreSQL through Npgsql/ADO.NET, with ordered SQL scripts and a local seed.
- OpenBao secrets read at startup; no secret or connection string in `appsettings*.json`.
- Aspire local environment: ephemeral PostgreSQL and OpenBao, schema/seed on every start, a Swagger sidecar with
  examples and a profile picker, and the dashboard.
- Unit, component, functional and architecture tests on Microsoft Testing Platform, with coverage thresholds.
- Husky.Net hooks: gitleaks, formatting, conventional commits and the pre-push gates.

## Requirements

| Tool | Version |
|---|---|
| .NET SDK | 10.0.1xx (`global.json`: 10.0.100, `latestFeature`) |
| Aspire CLI | 13.x |
| Docker Engine/Desktop | 29.x (or Podman with `ASPIRE_CONTAINER_RUNTIME=podman`) |
| HTTPS dev certificate | trusted for `localhost` |
| Node.js | only for the `npm run` scripts |

## Documentation

- [Repository README](https://github.com/theodj97/CleanWebApiTemplate)
- [Local development](https://github.com/theodj97/CleanWebApiTemplate/blob/main/local/local-development.md)
- [Tests](https://github.com/theodj97/CleanWebApiTemplate/blob/main/tests/README.md)
- [OpenAPI sidecar](https://github.com/theodj97/CleanWebApiTemplate/blob/main/local/openapi/README.md)

## License

MIT. See [LICENSE](https://github.com/theodj97/CleanWebApiTemplate/blob/main/LICENSE).
