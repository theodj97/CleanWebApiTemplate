# Repository instructions

This is an ASP.NET Core 10 API generated from CleanWebApiTemplate: Clean Architecture, Minimal APIs, Native
AOT, PostgreSQL through Npgsql, OpenBao for secrets and Aspire for local development. `README.md` covers the
quickstart; `GLOSSARY.md` the domain language; `docs/adr/` the decisions that are hard to reverse.

**For any code change, load the `clean-webapi` skill first.** It contains the detailed rules, the touch list
for a feature and the review checklist. This file only states the non-negotiables.

## Non-negotiables

1. **Layers point inward.** `Api → Application → Domain`, `Api → Infrastructure → Application`,
   `Infrastructure → Application/Domain`. `Application` never references Infrastructure or ASP.NET Core;
   `Domain` references nothing but `System.*` and `Ulid`.
2. **Native AOT is a gate.** No reflection on runtime paths, no `dynamic`, no reflection-based JSON, no
   Swashbuckle schema generation, no `RequiresDynamicCode`/`UnreferencedCode`/`UnconditionalSuppressMessage`.
   Fix `IL2xxx`/`IL3xxx` at the cause.
3. **Explicit registration.** No assembly scanning and no mediator. A use case is one
   `AddUseCase<TInput, TResult, THandler, TValidator>()`; a feature is one `Add{Feature}Module()`.
4. **Business outcomes are `Result<T>` with a typed `Error`.** No exceptions for validation, not-found or
   conflict.
5. **SQL is parameterised and identifier-allow-listed.** Values are bound; table and column names come from
   `{Feature}Table`; sort keys resolve through `{Feature}SortCatalog` and `{Feature}SortColumns`.
6. **The owner comes from the token, never from the body.** Use `ICurrentUser.Owner` and `OwnerScope`.
7. **Every secret comes from OpenBao in every mode.** By environment the application receives only
   `OpenBao:*`; never put a secret in `appsettings*.json`, a test or a report.
8. **Every change ships the right tests**: unit for logic and validators, component for HTTP and database,
   functional for a flow, architecture for structure.
9. **Every endpoint ships a local Swagger example** in `local/openapi/examples.json`.
10. **A change is done when the gates are green.** A green build is not a green AOT publish.

## Where code goes

A feature is one folder with its name in each layer:

```text
Domain/{Feature}/              entity, enum, constraints
Application/{Feature}/         port, DTO, filter, projection, sort catalog, shared validator
Application/{Feature}/{Verb}/  input, validator, handler
Infrastructure/{Feature}/      repository, table, sort-column map
Api/{Feature}/                 module, routes, endpoints, requests, responses, JSON context, OpenAPI
```

Shared code lives under `{Layer}/Common`. Tests mirror the production path under
`tests/CleanWebApiTemplate.UnitTests`, `.Api.ComponentTests` and `.Api.FunctionalTests`.

## Commands

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx
npm run test:fast   # unit, architecture and OpenAPI; no Docker
npm run verify      # the pre-push gates
npm run coverage    # coverage report and thresholds
aspire start        # PostgreSQL + OpenBao + API + Swagger UI
```

`global.json` selects Microsoft Testing Platform: use `--solution` or `--project`, never a positional path.
Component and functional tests need Docker. On Windows use `win-x64` for the publish.

## Secrets

`dotnet local/tools/devtoken.cs init` creates the local HS256 key in the AppHost user secrets. `openbao-init`
writes it and the PostgreSQL connection string into OpenBao; the application reads both from there. The root
token is generated per start. Never commit a key or a token; never log one.

## Hooks and commits

- `pre-commit`: gitleaks on the staged diff and `dotnet format` on the staged `.cs`.
- `commit-msg`: `<type>(<scope>): <description>`, types `feat`, `fix`, `refactor`, `docs`, `test`, `chore`,
  `build`, `perf`, `revert`.
- `pre-push`: `npm run verify` (build, format, analyzers, every test, vulnerable packages).

Skip them only when the user asks: `HUSKY=0 git …` or `--no-verify`.

## Documentation

- `README.md` — quickstart, configuration keys, API table, tests, hooks.
- `local/local-development.md` — Aspire topology, seed, devtoken, troubleshooting.
- `local/openapi/README.md` — Swagger sidecar, catalog and profiles.
- `tests/README.md` — test projects, identities and conventions.
- `docs/adr/` — decisions; add one when a choice is hard to reverse and surprising without context.
- `CHANGELOG.md` — `[Unreleased]` for user-visible changes.
