# Glossary: technical vocabulary

The domain language lives in `GLOSSARY.md` at the repository root. This file defines the technical words the
architecture uses, so a review and a handler name the same concept the same way.

## Layers and composition

**Domain**: entities, enums and business constraints. No framework, no HTTP, no SQL.
_Avoid_: model, core (as a project name)

**Application**: use cases, ports, DTOs, validation and `Result`. It answers "what can the system do".
_Avoid_: business layer, services

**Infrastructure**: adapters for PostgreSQL and OpenBao. It answers "how the system reaches its dependencies".
_Avoid_: data layer, DAL

**Api**: the composition root and the HTTP adapter. It owns routing, authentication, JSON, OpenAPI and
ProblemDetails.
_Avoid_: presentation, controller layer

**Composition root**: the place that wires implementations (`ConfigureServices` plus each feature module).
_Avoid_: bootstrap, container config

**Module**: a `{Feature}Module` extension that registers everything one feature needs: repository, use cases,
routes, JSON context and OpenAPI transformer.
_Avoid_: installer, registration class

**Port**: an interface the Application defines and Infrastructure implements (`ITodoRepository`).
_Avoid_: repository interface (too narrow), gateway

**Endpoint adapter**: `{Feature}Endpoints`, the static class that maps a request to an input, calls a handler
and converts the result.
_Avoid_: controller, handler (reserved for the Application)

## Use cases

**Handler**: `IHandler<TInput, TResult>`, one class per use case. Validates, applies the business rule and
returns `Result<T>`.
_Avoid_: command handler, service, use case (the class is the use case)

**Input**: the Application contract of a use case (`CreateTodoInput`). It carries the owner scope, never the
HTTP shape.
_Avoid_: command, DTO, model

**Request**: the HTTP body (`CreateTodoRequest`). It maps to an Input through `ToInput(OwnerScope)`.
_Avoid_: command, payload, model

**Response**: the HTTP body returned (`TodoResponse`). It maps from a DTO or projection.
_Avoid_: view model, output

**DTO**: the Application read model (`TodoDto`), flat and non-nullable where the entity guarantees a value.
_Avoid_: model, entity, view model

**Projection**: a deliberately partial read (`TodoTitle`), returned by a dedicated port method.
_Avoid_: summary, partial DTO

**Filter**: the persistence-neutral query contract (`TodoFilter`) built by a filter input.
_Avoid_: query object, criteria

**ValidationService**: the base class that runs `ValidatorSet<TInput>` and skips the action on failure.
_Avoid_: pipeline, decorator, middleware

**ValidatorSet**: every validator registered for one input, aggregated and never empty.
_Avoid_: validator collection, composite validator

**Startup check**: the `IStartupCheck` registration that builds a use case once at startup so a missing
validator fails before the first request.
_Avoid_: warm-up, health check

## Results and errors

**Result**: `Result<T>` — a value or a typed `Error`, never both.
_Avoid_: response, outcome, Either

**Error**: the typed business failure (`BadRequestError`, `NotFoundError`, `ConflictError`, …). It has no HTTP
semantics; the Api maps it.
_Avoid_: exception, problem, failure (as a type)

**ProblemDetails**: the RFC 9457 body the API returns for 4xx/5xx, built by `ProblemDetailsFactory`.
_Avoid_: error response, error model

## Ownership and access

**Owner**: the caller who created an item; fixed at creation.
_Avoid_: creator, author

**OwnerScope**: the value type that turns the caller into `VisibleOwner`, `OwnerForCreate` and `FilterOwners`.
_Avoid_: user context, tenant

**VisibleOwner**: `null` for admins, the caller email otherwise; the owner filter passed to a port.
_Avoid_: createdBy filter

**Owners**: the owner list of a filter request (`owners` in JSON). Admins keep it; everyone else is pinned.
_Avoid_: createdBy list

**LastWriter**: the caller stored as `UpdatedBy` when an update changes something.
_Avoid_: editor, modifier

**Policy**: a named authorization rule in `Constants` (`User`, `Operator`, `Admin`, `External`).
_Avoid_: role (the role is the claim value; the policy is the rule)

## Persistence

**Data source**: the singleton `NpgsqlDataSource` that owns the connection pool.
_Avoid_: connection factory, DbContext

**Table helper**: `{Feature}Table`, the constants for the table, columns and index names.
_Avoid_: schema, metadata

**Sort catalog**: the accepted sort keys, in Application.
_Avoid_: sort options

**Sort columns**: the whitelist map from key to physical column, in Infrastructure.
_Avoid_: column map

**Seed**: synthetic rows in `local/database/seed/*.sql`, applied only in Local mode.
_Avoid_: fixtures, sample data (in code)

**Forward-only script**: a numbered SQL file that is never edited after it is deployed; a change is a new
file.
_Avoid_: migration (there is no migration runner)

## JSON and OpenAPI

**JSON context**: a source-generated `JsonSerializerContext` for a feature's contracts or a shared payload.
_Avoid_: serializer, converter

**Document**: the OpenAPI document at `/openapi/v1.json`, built by `OpenApiConfiguration` and the feature
transformers.
_Avoid_: Swagger (Swagger is the UI)

**Schema descriptor**: `{Feature}OpenApiSchemas`, the static class that adds descriptions, formats and enums
to the generated schemas.
_Avoid_: schema builder, Swagger config

**Examples catalog**: `local/openapi/examples.json` plus `bodies/` and `responses/`, merged by the sidecar.
_Avoid_: Swagger examples (the catalog is separate from the document)

## Tests

**Fixture**: the shared AppHost environment for a test project (`ComponentTestFixture`, `FunctionalTestFixture`).
_Avoid_: test server, harness

**Component test**: in-process API against the real containers.
_Avoid_: integration test (there is a separate functional suite)

**Functional test**: the API as a separate process, HTTP only.
_Avoid_: end-to-end test

**Reset**: emptying the databases before a test (`[ResetDatabase]`, Respawn).
_Avoid_: cleanup, teardown

**Builder**: a test data builder with valid defaults (`TodoBuilder`).
_Avoid_: factory, mother

## Decisions

The reasons behind the patterns are recorded as ADRs in `docs/adr/`:

| ADR | Decision |
|---|---|
| 0001 | Native AOT for the API |
| 0002 | ADO.NET/Npgsql instead of an ORM |
| 0003 | Result/Error instead of exceptions for business outcomes |
| 0004 | Explicit registration and feature modules |
| 0005 | Every secret comes from OpenBao |
| 0006 | Ephemeral Aspire environment |
| 0007 | ULID stored as `bytea` |
| 0008 | Forward-only SQL scripts |
| 0009 | Owner identified by the email claim |
