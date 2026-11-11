# Architecture

Five runtime projects, one dependency rule, and a request that walks from the endpoint to PostgreSQL and back.
This reference explains the shape and the reasons behind it, so a change lands in the right layer instead of
only compiling.

## Projects and responsibilities

| Project | Owns | May reference |
|---|---|---|
| `CleanWebApiTemplate.Domain` | Entities, enums, business constraints, value objects. | `System.*`, `Ulid` |
| `CleanWebApiTemplate.Application` | Use cases, inputs, validators, results, DTOs, projections, ports, sort catalog, `ICurrentUser`/`OwnerScope`. | Domain, `FluentValidation` |
| `CleanWebApiTemplate.Infrastructure` | ADO.NET repositories, table and sort-column maps, OpenBao configuration source. | Application, Domain, `Npgsql`, `Microsoft.Extensions.Configuration` |
| `CleanWebApiTemplate.Api` | Composition root, routes, endpoint adapter, HTTP contracts, JSON contexts, OpenAPI, auth, ProblemDetails. | Application, Infrastructure, ServiceDefaults |
| `CleanWebApiTemplate.ServiceDefaults` | OpenTelemetry wiring, `/health` and `/alive`, base health checks. | ASP.NET Core shared framework |

`Application` never references `Infrastructure` or ASP.NET Core: it defines the ports that Infrastructure
implements, and the Api supplies the caller through `ICurrentUser`. `Domain` has no framework or HTTP
concept.

The rule is enforced by `tests/CleanWebApiTemplate.ArchitectureTests`:

- `DependencyRulesTests` loads the four assemblies and fails when a layer depends on an outer one, when
  `Application` references the Infrastructure assembly, or when `Application`/`Domain` reference
  `Microsoft.AspNetCore`.
- `StructureTests` enforces the folder conventions: handlers are sealed and live under
  `Application.{Feature}.{UseCase}`, shared contracts under `Application.Common`, infrastructure feature
  types are repositories/table/sort maps, route groups live under `Api.{Feature}`.

`BannedSymbols.txt` (wired into every `src/` project) blocks reflection and reflection-based JSON at compile
time. It does not check dependency direction; the architecture tests do.

## Folder map

```text
src/CleanWebApiTemplate.Domain/
├── Common/                 BaseEntity, IBaseEntity
└── Todo/                   TodoEntity, ETodoStatus, TodoConstraints

src/CleanWebApiTemplate.Application/
├── Common/
│   ├── Handlers/           IHandler<TInput, TResult>
│   ├── Identity/           ICurrentUser, OwnerScope
│   ├── Results/            Result<T>, Error, IResultFactory<T>
│   └── Validation/         BaseAbstractValidator, ValidationService, ValidatorSet, DateRange
└── Todo/
    ├── ITodoRepository.cs  the port
    ├── TodoDto.cs          read model returned by the API
    ├── TodoDtoExtensions.cs
    ├── TodoFilter.cs       persistence-neutral query contract
    ├── TodoTitle.cs        id/title projection
    ├── TodoSortCatalog.cs  accepted sort keys
    ├── TodoValidator.cs    shared rules for the feature
    ├── Create/             CreateTodoInput, CreateTodoInputValidator, CreateTodoHandler
    ├── GetById/
    ├── Filter/
    ├── Update/
    ├── Delete/
    └── GetTitles/

src/CleanWebApiTemplate.Infrastructure/
├── Secrets/                OpenBao client, source, provider, options, keys
└── Todo/                   TodoRepository, TodoTable, TodoSortColumns

src/CleanWebApiTemplate.Api/
├── Common/
│   ├── Abstractions/       IGroupMap, route-group helpers
│   ├── Composition/        AddUseCase, startup checks
│   ├── Responses/          IBaseResponse<TDto, TResponse>
│   └── HttpContextCurrentUser.cs
├── Configuration/          AppSettings, auth, JSON context, OpenAPI, ProblemDetails, exception handler
├── Helpers/                AppConfigurationHelper
├── Extensions/             ApiResultExtensions, EndpointExtension
├── Todo/                   module, routes, endpoints, responses, requests, JSON context, OpenAPI schemas
├── ConfigureServices.cs    composition root
├── ConfigureApplication.cs middleware pipeline
└── Program.cs
```

A feature is **one folder with its name in each layer**. Removing the sample is deleting those folders plus the
module call, the SQL scripts and the tests. See `feature-slice.md`.

## ServiceDefaults

`CleanWebApiTemplate.ServiceDefaults` is shared runtime code, not a layer:

- `AddServiceDefaults()` configures OpenTelemetry: logging, metrics (ASP.NET Core, HTTP client, runtime) and
  traces (the app source, ASP.NET Core, HTTP client). The OTLP exporter is added only when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so tests and plain runs export nothing.
- `/health` and `/alive` are excluded from traces (probe noise).
- `AddDefaultHealthChecks()` adds the `self` check tagged `live`; `MapDefaultEndpoints()` maps `/health`
  (aggregate) and `/alive` (the `live` tag). The API adds `api-health-check` (`/health/api`) and
  `postgres-check` (`/health/postgresDb`) in `ConfigureServices`.

## Request lifecycle

1. Kestrel receives the request. `Program` built the host with `CreateSlimBuilder`, `ValidateScopes` and
   `ValidateOnBuild`, and `UseKestrelHttpsConfiguration()` (the slim builder leaves HTTPS wiring out).
2. `ConfigureApplication` runs `UseExceptionHandler`, response compression, HTTPS redirection,
   `UseStatusCodePages` (ProblemDetails for body-less 4xx/5xx), CORS, authentication and authorization.
3. The route group's policy runs. `HasEmailClaim` rejects a token without `email` with 403.
4. The request body is deserialized by the source-generated request delegate with the app's
   `JsonSerializerOptions`, whose resolver chain starts with the feature contexts and then
   `AppJsonSerializerContext` (ProblemDetails).
5. `{Feature}Endpoints` builds the input: request mapping plus `ICurrentUser.Owner` (`OwnerForCreate`,
   `VisibleOwner` or `FilterOwners`). The owner never comes from the body.
6. The handler runs. `ValidationService.ExecuteAsync` validates through `ValidatorSet<TInput>`; a failure
   returns `BadRequestError("Validation Errors", …)` and the action is skipped.
7. The handler calls the port, maps the entity to a DTO and returns `Result<TDto>`.
8. `ApiResultExtensions` maps success to the documented 2xx and every error type to its status;
   `ProblemDetailsFactory` builds the body with `traceId`.
9. The response is serialized through the feature's `JsonSerializerContext`.

## Result and Error

`Result<T>` is a value or a typed `Error`, never both:

```csharp
Result<TodoDto>.Success(dto);
Result<TodoDto>.Failure(new NotFoundError("ID was not found."));
```

`Error` subtypes and their HTTP mapping:

| Error | Status |
|---|---|
| `BadRequestError`, `DomainError` | 400 |
| `UnauthorizedError` | 401 |
| `ForbiddenError` | 403 |
| `NotFoundError` | 404 |
| `ConflictError` | 409 |

An unmapped subtype throws `ArgumentOutOfRangeException`, which the exception handler turns into a 500. Add a
new subtype only when it maps to a new status, and update `ApiResultExtensions.ResolveError` in the same
change.

## Patterns and why

**Handler per use case.** `IHandler<TInput, TResult>` plus `ValidationService` gives one place per use case,
no mediator and no assembly scanning. A handler is a sealed class with a primary constructor.

**Validator set.** `ValidatorSet<TInput>` collects every registered `IValidator<TInput>`, refuses to be
constructed empty and aggregates failures (parallel validators, duplicate messages removed). `AddUseCase`
registers it, and `Program` builds every use case at startup, so a missing validator stops the host.

**Ports in the feature.** `ITodoRepository` lives in `Application.Todo` because it speaks the feature's
language. `Infrastructure.Todo.TodoRepository` implements it with ADO.NET; there is no EF Core, no Dapper and
no generic repository.

**ADO.NET by hand.** AOT forbids reflection-based mapping, and the template wants the SQL visible. Every
column is mapped explicitly, every value is a bound parameter, every identifier comes from `{Feature}Table`.

**Explicit DI.** `ConfigureServices` calls one module per feature; modules call `AddUseCase<…>()`. A new
service is a new registration, never a convention. `ValidateOnBuild` catches a missing dependency of a
registered service; `ValidateUseCases` catches a handler whose validator set is empty.

**JSON source generation.** Each feature has a `{Feature}JsonSerializerContext`; `AppJsonSerializerContext`
holds the shared ProblemDetails. The contexts are chained in `ConfigureHttpJsonOptions`. A contract missing
from the context fails the `JsonContractTests` unit test and, at runtime, the AOT binary.

**Owner scope as a value type.** `OwnerScope` turns "who is calling" into the three questions the code needs:
what may I see (`VisibleOwner`), who owns what I create (`OwnerForCreate`) and which owners may I filter
(`FilterOwners`). The rule lives in one type instead of in every mapper.

## Where does this code go?

| You are adding | Layer | Folder |
|---|---|---|
| An entity, enum, business limit | Domain | `Domain/{Feature}` |
| A use case, its input and validator | Application | `Application/{Feature}/{UseCase}` |
| A query the database must answer | Application | `Application/{Feature}` as a port method, or a filter record |
| A read model returned by the API | Application | `Application/{Feature}` as `{Feature}Dto` |
| A partial read for a projection endpoint | Application | `Application/{Feature}` as `{Thing}` record |
| A sort vocabulary | Application | `Application/{Feature}/{Feature}SortCatalog.cs` |
| A SQL query, table constant, sort-column map | Infrastructure | `Infrastructure/{Feature}` |
| A route, endpoint adapter, request, response | Api | `Api/{Feature}` |
| A JSON context for the feature | Api | `Api/{Feature}/{Feature}JsonSerializerContext.cs` |
| OpenAPI schema descriptions for the feature | Api | `Api/{Feature}/{Feature}OpenApiSchemas.cs` |
| A shared HTTP helper (ProblemDetails, Result mapping, route groups) | Api | `Api/Common`, `Api/Extensions` |

If a type does not fit, the question is usually "which layer owns the concept", not "where can it compile".
