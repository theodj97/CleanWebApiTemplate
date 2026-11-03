# Changelog

All notable changes to the Clean Web API Template are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-11-03

### Added

- `dotnet new` template (`clean-webapi`) for an ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT,
  JWT authentication, PostgreSQL, OpenBao and Aspire.
- Unit, component, functional and architecture tests with coverage thresholds.
- Local Aspire environment with PostgreSQL, OpenBao, a schema/seed initializer and a Swagger sidecar.
- Husky.Net hooks: gitleaks, formatting, conventional commits and the pre-push gates.
- NuGet packaging project (`build/CleanWebApiTemplate.Template.csproj`) and package smoke test.

[Unreleased]: https://github.com/theodj97/CleanWebApiTemplate/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/theodj97/CleanWebApiTemplate/releases/tag/v1.0.0
