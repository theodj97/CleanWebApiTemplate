# Changelog

All notable changes to the Clean Web API Template are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Fast test commands: `npm run test:fast` (unit, architecture and OpenAPI, no Docker) and the per-suite
  `test:unit`, `test:architecture`, `test:openapi`, `test:component` and `test:functional` scripts.

### Changed

- `npm run verify` runs the independent gates (format/style/analyzers in one `dotnet format` pass, every test and
  the vulnerable-package check) concurrently after the build and replays their output: ~78 s to ~25 s on an
  8-core Linux machine, with the same gates and no output hidden.
- Test modes reuse one PostgreSQL container per mode and checkout (stable name, volume and derived local
  credential; `TESTCONTAINERS_REUSE=0` opts out). The idempotent schema still runs on every start and the fixture
  resets rows once at startup; the full test suite drops from ~18 s to ~12 s when the containers are warm.
- `npm run coverage` collects the unit, component and functional suites in parallel.
- The vulnerable-package gate uses one JSON query instead of running `dotnet list` twice.
- `docs/adr/0010` records the reused test database; `0006` notes the exception for test modes.

## [2.0.0] - 2026-11-12

### Added

- Agent layer shipped with the template: `AGENTS.md`, `GLOSSARY.md`, `docs/adr/`, the `clean-webapi` skill with a
  complete `Project` example, the `/feature`, `/endpoint`, `/verify` and `/remove-sample` commands and a read-only
  reviewer subagent. The maintainer-only `template-maintainer` skill stays in the template repository.
- `AddUseCase<TInput, TResult, THandler, TValidator>()`, one composition module per feature and startup checks that
  build every use case before the first request.
- One `JsonSerializerContext` per feature and `JsonContractTests` to keep the contracts registered.
- `Project` example (CRUD, projection, SQL, seed, OpenAPI examples and tests) verified by the template smoke test.

### Changed

- Every layer is organised feature-first (`Domain/Todo`, `Application/Todo/...`, `Infrastructure/Todo`,
  `Api/Todo`) with shared code under `Common`; the architecture tests follow the convention instead of naming Todo.
- The contract uses 404 for a missing or invisible row, `200 []` for an empty list and 204 for a successful delete;
  routes are lowercase and plural (`/api/todos`).
- `Api.UnitTests` is now `UnitTests` and mirrors the production path.
- The PostgreSQL connection string is read from OpenBao in every mode; `NpgsqlDataSource` replaces the connection
  factory.
- Environment files are named after `ASPNETCORE_ENVIRONMENT` (`appsettings.Production.json`, …); the previous
  upper-case names did not load on Linux.
- Aspire packages are 13.6.5 (GHSA-f7vh-h5g3-7jrh).

### Fixed

- The startup comment promised `ValidateOnBuild` would reject an empty validator set; the startup check now really
  does.
- `DbInitializer` no longer queries the `Todo` table, so removing the sample does not break `aspire start`.
- The OpenAPI helpers no longer hardcode Todo sort keys or statuses.
- `BannedSymbols.txt` covers `MakeGenericType`, `Assembly.GetTypes`, `Enum.GetValues(Type)` and the reflection JSON
  overloads.
- `Enum.IsDefined` is generic, the ULID validator reports one failure, the failure message typo and the stale
  comments are gone.

## [1.1.0] - 2026-11-03

### Added

- `GetTodoById` and `GetTodoTitles` use cases, with the `TodoTitle` projection and its response.
- `IHandler<TInput, TResult>`, `ValidatorSet<T>` and `ValidationService` as the explicit use-case and
  validation contracts.
- `OwnerScope` and the identity abstractions (`ICurrentUser`), so ownership rules live in one value type.
- `TodoSortCatalog` and `TodoSortColumns`, with an alignment test that keeps the accepted keys and the SQL
  column map in sync.
- `TodoEndpoints`, the endpoint adapter that keeps `TodoRoutes` declarative.
- Title uniqueness per owner, pre-checked in the handler and backed by the unique index (`409 Conflict`).

### Changed

- Handlers moved to `{Feature}/Handlers/{Verb}` and implement `IHandler<TInput, TResult>`; the per-use-case
  `I{Verb}TodoService` interfaces were removed.
- Validation runs through `ValidatorSet<T>`; a missing registration now fails at startup through
  `ValidateOnBuild`.
- Query filters, projections and DTO mapping moved to their documented folders.
- Tests and the OpenAPI/ProblemDetails fixtures updated for the new handler and validation contracts.

### Removed

- `ValidationRunner` and the per-use-case service interfaces.

## [1.0.0] - 2026-11-03

### Added

- `dotnet new` template (`clean-webapi`) for an ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT,
  JWT authentication, PostgreSQL, OpenBao and Aspire.
- Unit, component, functional and architecture tests with coverage thresholds.
- Local Aspire environment with PostgreSQL, OpenBao, a schema/seed initializer and a Swagger sidecar.
- Husky.Net hooks: gitleaks, formatting, conventional commits and the pre-push gates.
- NuGet packaging project (`build/CleanWebApiTemplate.Template.csproj`) and package smoke test.

[Unreleased]: https://github.com/theodj97/CleanWebApiTemplate/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/theodj97/CleanWebApiTemplate/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/theodj97/CleanWebApiTemplate/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/theodj97/CleanWebApiTemplate/releases/tag/v1.0.0
