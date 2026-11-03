# Changelog

All notable changes to the Clean Web API Template are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/theodj97/CleanWebApiTemplate/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/theodj97/CleanWebApiTemplate/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/theodj97/CleanWebApiTemplate/releases/tag/v1.0.0
