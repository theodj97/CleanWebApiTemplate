# Architecture

## The five runtime projects

```text
src/
├── {App}.Api              Minimal API: routes, authentication, ProblemDetails, OpenAPI, JSON contracts, composition root
├── {App}.Application      Use cases, validators, Result<T>/Error, DTOs, repository ports, query filters
├── {App}.Domain           Entities, enums, constraints; no framework references
├── {App}.Infrastructure   Npgsql repositories, connection factory, OpenBao configuration source
└── {App}.ServiceDefaults  OpenTelemetry and base health checks, shared by every host

local/                     AppHost, DB initializer, OpenBao initializer, Swagger sidecar, devtoken
tests/                     Unit, component, functional and architecture tests, plus Tests.Shared
```

## Dependency rule

```text
Api ──────────► Application ──────────► Domain
 │                   ▲
 │                   │
 └──────────► Infrastructure ──────────► Domain
```

| Project | May reference | Must not reference |
|---|---|---|
| `Domain` | `System.*` only | Application, Infrastructure, Api, ServiceDefaults, ASP.NET Core |
| `Application` | Domain | Infrastructure, Api, ServiceDefaults, ASP.NET Core |
| `Infrastructure` | Application, Domain | Api, ServiceDefaults |
| `Api` | Application, Infrastructure, ServiceDefaults | — |
| `ServiceDefaults` | ASP.NET Core and OpenTelemetry packages | Api, Application, Infrastructure, Domain |
| `tests/*` | every runtime project and `local/` (the AppHost) | — |
| `local/*` | runtime projects | — |

The rule is enforced twice:

- `tests/{App}.ArchitectureTests/DependencyRulesTests.cs` (ArchUnitNET) checks namespaces and assembly
  references.
- `BannedSymbols.txt` + `Microsoft.CodeAnalysis.BannedApiAnalyzers` block reflection and reflection-based
  JSON.

When a change makes a new project or namespace, add the matching architecture test. When a change needs a
type from an outer layer, the design is wrong: move the abstraction inward or the implementation outward.

## Layer responsibilities

| Layer | Put here | Never here |
|---|---|---|
| **Domain** | `{Feature}Entity`, enums (`E{Feature}Status`), `{Feature}Constraints`, value objects, pure domain rules | DTOs, HTTP status codes, configuration, SQL, attributes from ASP.NET, `Result<T>` |
| **Application** | Inputs, validators, handlers, shared service interfaces, `Result<T>`/`Error`, DTOs, DTO mapping, repository ports, query filters | `HttpContext`, `IResult`, `IActionResult`, `Npgsql*`, connection strings, OpenBao, `ILogger` as a business dependency |
| **Infrastructure** | Repository implementations, SQL text, row mapping, connection factory, OpenBao configuration provider | Business rules, validation, DTO-to-response mapping, HTTP concepts |
| **Api** | Request/response contracts, `ToInput`, route groups, endpoint metadata, authentication/authorization, ProblemDetails, OpenAPI, JSON context, composition root | Business rules, SQL, direct `NpgsqlConnection` use, entity persistence |
| **ServiceDefaults** | OpenTelemetry, health check defaults, host-level cross-cutting setup | Feature code, domain logic |

## Request lifecycle

1. `Program.Main` creates a slim builder, loads `AppSettings`, calls `AddApiServices`, builds, configures the
   pipeline, maps routes and runs.
2. `ConfigureApplication` adds the exception handler, response compression, HTTPS redirection, status-code
   pages, CORS, authentication and authorization, then maps OpenAPI and the route groups.
3. A route group (`IGroupMap`) maps its `{Feature}Endpoints` methods with a policy. The endpoint adapter owns the
   request → input → handler → response plumbing; the route group owns only templates and metadata.
4. The request contract maps to an application input: `request.ToInput(currentUser.Owner)`. The owner always
   comes from the token; `OwnerScope` decides `VisibleOwner`/`OwnerForCreate`/`FilterOwners`.
5. The application handler calls `ValidationService.ExecuteAsync`, which runs the input's registered
   `ValidatorSet<TInput>`. The set refuses an empty registration; a missing set fails at startup through
   `ValidateOnBuild`. A validation failure becomes a `BadRequestError`; the action is skipped.
6. The handler applies business rules, calls the repository port, and returns `Result<T>` with a `Dto`,
   projection or an `Error`.
7. `ApiResultExtensions` maps the result: success to `200`/`204`/`201`, failure to `400`/`401`/`403`/`404`/
   `409` ProblemDetails. `ProblemDetailsFactory` builds the body (`type` = `https://httpstatuses.io/{status}`,
   `traceId` extension); the mapper does not shape its own body.
8. `GlobalExceptionHandler` catches unexpected exceptions and malformed JSON; `UseStatusCodePages` gives
   unmatched routes and methods the same ProblemDetails contract.

## Patterns and their reasons

### Result and Error

`Result<T>` and the `Error` hierarchy live in `Application/Results`, not Domain. A handler returns
`Result<T>.Success(value)` or `Result<T>.Failure(new NotFoundError(...))`. The API chooses the HTTP status in
`ApiResultExtensions.ResolveError`; every ProblemDetails body is built by `ProblemDetailsFactory`.

Why: business failures are expected outcomes, not exceptional control flow; the translation to HTTP belongs to
the API, so Application stays free of ASP.NET.

Rules:

- Add a new error type only when it maps to a new HTTP status. Extend `ResolveError` (which delegates to
  `ProblemDetailsFactory`) and the OpenAPI `ProducesProblem` metadata in the same change.
- Do not throw for validation, not-found or conflict. Throw only for programmer errors (null where the
  contract forbids it, impossible state).
- Repositories return `Result<T>` when a database constraint is a business outcome (duplicate title/name);
  otherwise `null` or `bool`.

### Handler

- One handler per use case: `{Verb}{Feature}Handler` is `sealed`, lives next to its input and validator, and
  implements `IHandler<TInput, TResult>`.
- `IHandler<TInput, TResult>` exposes `Task<Result<TResult>> HandleAsync(TInput input, CancellationToken
  cancellationToken)`. The `Result<T>` contract stays in the interface, so every use case has the same outcome
  shape.
- Handlers are registered explicitly by their closed generic interface; the route delegates inject
  `IHandler<TInput, TResult>`.
- Shared collaborators belong in `Application/Abstractions/Services`; persistence ports belong in
  `Application/Abstractions/Repositories`; the caller context (`ICurrentUser`) belongs in
  `Application/Abstractions/Identity`.

### Shared services

- A service is a collaborator shared by several handlers, not a use case. Its interface lives in
  `Application/Abstractions/Services`; its implementation lives next to the feature that owns it.
- Extract a service only when a second handler needs the same behaviour. A single handler keeps the logic
  in its `HandleAsync`.
- The handler still owns validation, business outcomes and `Result<T>`; the service only provides the
  shared behaviour.
- Register the concrete service explicitly in `ConfigureServices`.

### Validation

- Input shape: `AbstractValidator<TInput>` in Application, usually deriving from the feature's shared
  `{Feature}Validator<TInput>`.
- `BaseAbstractValidator<TRequest>` lives in `Application/Validation` and provides `NotNullNotEmpty`,
  `ValidateUlid`, `ValidateDateTime`, `ValidateStartDateAndEndDate`, `ValidateSortBy`, `IsValidEmail` and
  `AddFailure`.
- `ValidatorSet<TInput>` (also `Application/Validation`) is the handler's validation dependency. Its
  constructor refuses an empty `IEnumerable<IValidator<TInput>>`; `Program.Main` sets
  `UseDefaultServiceProvider(ValidateOnBuild = true)`, so a handler with no registered set — or a set with
  no validator — fails at startup instead of silently accepting unchecked input.
- `ValidationService.ExecuteAsync` takes the `ValidatorSet<TInput>` and returns the aggregated failures as
  `BadRequestError`.
- Uniqueness, ownership and state transitions are **business rules** in the handler, because they need the
  repository and can become `ConflictError`/`NotFoundError`.

Why no validation decorators or MediatR: the template keeps the call path explicit and AOT-safe. A handler
inherits `ValidationService`; there is no pipeline, no assembly scan and no dynamic dispatch.

### Repository

- The port (`I{Feature}Repository`) and the query filter (`{Feature}Filter`) live in Application.
- The adapter (`{Feature}Repository`) lives in Infrastructure, uses `INpgsqlConnectionFactory` and plain
  `NpgsqlCommand`/`NpgsqlDataReader`.
- Every row is mapped manually (`Map{Feature}`), in the exact column order of the shared `{Feature}Columns`
  constant.
- A connection is created, opened and disposed per operation; Npgsql pools underneath.
- There is no EF Core, no Dapper, no expression trees, no reflection mapping. This is deliberate: Native AOT
  and predictable SQL.

### DTO, projection and response mapping

- Entity → DTO: `Application/Dtos/{Feature}/{Feature}DtoExtensions.ToDto`, internal to Application.
- Partial projection: when a query selects only some fields, define `{Projection}` in
  `Application/{Feature}/Projections`, return it from the port and carry it through the handler. Do not return
  a partially filled entity/DTO and do not narrow it again with `!` casts at the response edge. The template's
  `TodoTitle` is the worked example.
- DTO/projection → response: `Api/Models/Responses/{Feature}/{Feature}Response.ToResponseModel`, through
  `IBaseResponse<TDto, TResponse>`.
- The API never returns an entity or a DTO directly; the response type is the public contract and the OpenAPI
  schema.

### Composition root

`Api/ConfigureServices.cs` is the only place that knows concrete Application and Infrastructure types. It
registers validators, one `ValidatorSet<TInput>` per input, handlers and repositories one by one.
`Api/Extensions/EndpointExtension.cs` registers each `IGroupMap` with `TryAddEnumerable`. There is no assembly
scanning, because the AOT analyzer must see every type statically. `ValidateOnBuild` turns a missing
`ValidatorSet<T>` registration into a startup failure.

### Current user

`ICurrentUser` (`Application/Abstractions/Identity`) exposes `Email`, `IsAdmin` and `OwnerScope Owner`.
`HttpContextCurrentUser` (Api) reads the claims from `IHttpContextAccessor` and builds the value with
`new(Email, IsAdmin)`. `OwnerScope` is a value type, not a nullable string:

- `VisibleOwner` is `null` for admins and the email for everyone else (read/filter/delete scope).
- `OwnerForCreate` is always the caller's email, even for admins.
- `FilterOwners(requested)` keeps the requested owners for admins and pins everyone else to their email.

Application code consumes `OwnerScope`; it never reads claims or `HttpContext`.

### Configuration

`AppSettings` is bound manually in `AppConfigurationHelper.LoadAppSettings` (no reflection binder). A section
class derives from `SectionBase` and its name must end in `Section` (`ConnectionStringsSection` →
`ConnectionStrings`). `AppSettingsValidator` fails startup with a clear message.

### Secrets

`AppSettings` holds no secret. OpenBao is a configuration source added by
`Infrastructure/Secrets/OpenBaoConfigurationExtensions.AddOpenBaoSecrets`; the API reads `API_KEY` from it in
Symmetric mode. The AppHost seeds the store with `openbao-init`; the application only receives `OpenBao:*`
(address, token, mount, path), never the secret values as environment variables.

### Native AOT

The API declares `PublishAot`, `IsAotCompatible`, `EnableAotAnalyzer`, `EnableRequestDelegateGenerator` and
`GenerateDocumentationFile`. JSON goes through `AppJsonSerializerContext`; OpenAPI is shaped by static
descriptors; request delegates are source-generated. See `native-aot.md`.

## Where does this code go?

| You are adding... | Layer | Folder |
|---|---|---|
| A new field with validation | Domain (constraint/entity) + Application (input/validator) + API (request/response) + SQL | `Domain/Models`, `Application/{Feature}/Handlers/{Verb}`, `Api/Routes/{Feature}/{Verb}` |
| A new business rule that needs the database | Application handler | `Application/{Feature}/Handlers/{Verb}/{Verb}{Feature}Handler.cs` |
| Shared behaviour used by several handlers | Application service | `Application/Abstractions/Services` (interface), implementation next to the feature |
| A new SQL query | Application port + Infrastructure repository | `Application/Abstractions/Repositories`, `Infrastructure/Repositories/{Feature}` |
| A new endpoint over an existing handler | API only | `Api/Routes/{Feature}/{Feature}Endpoints.cs`, `{Feature}Routes.cs` |
| A new status code | Application `Error` + `ApiResultExtensions.ResolveError` + OpenAPI metadata | `Application/Results/Error.cs`, `Api/Extensions/ApiResultExtensions.cs` |
| A new sort key | Application sort catalog + Infrastructure column map + alignment test | `Application/{Feature}/Sorting`, `Infrastructure/Repositories/{Feature}/Helpers` |
| A partial read projection | Application projection type + port + handler + response | `Application/{Feature}/Projections`, `Api/Models/Responses/{Feature}` |
| A new external service | Infrastructure adapter + Application port | `Infrastructure/{Concern}`, `Application/Abstractions/Services` |
| A new configuration value | API `AppSettings` + validator + appsettings + AppHost | `Api/Configuration`, `local/aspire` |
| A new secret | OpenBao initializer + AppHost + docs | `local/aspire`, `local/local-development.md` |
| A new database table | SQL script + Infrastructure repository + tests | `local/database`, `Infrastructure/Repositories` |
| A new cross-cutting host concern | ServiceDefaults or API pipeline | `src/{App}.ServiceDefaults`, `Api/ConfigureApplication.cs` |

## Common mistakes

- Putting `Result<T>` or `HttpStatusCode` in Domain.
- Referencing `Microsoft.AspNetCore.*` from Application to reuse `IResult`.
- Using EF Core, Dapper or a mapper library because it is faster to write.
- Registering handlers or validators with assembly scanning or `AddValidatorsFromAssembly`.
- Reading `HttpContext` or claims in Application.
- Accepting `createdBy`/`owner` from the request body.
- Treating `OwnerScope` as a nullable string instead of using `VisibleOwner`/`OwnerForCreate`/`FilterOwners`.
- Building SQL with string interpolation of user input.
- Adding a sort key without the feature catalog and the SQL map; the alignment test must fail until both agree.
- Returning a partially filled entity/DTO and hiding the missing fields with `!` casts; add a projection type.
- Taking `IEnumerable<IValidator<T>>` in a handler instead of `ValidatorSet<T>`; an empty set no longer means
  "valid".
- Adding repository helpers for a second feature without generalizing the hardcoded `Todo.Helpers` condition in
  `StructureTests`; the architecture test fails even though the layout is correct.
- Forgetting `AppJsonSerializerContext` (the endpoint compiles but fails at runtime under AOT).
- Forgetting the OpenAPI schema descriptor (the document loses descriptions and formats).
- Suppressing an AOT warning instead of fixing the cause.
- Adding a package without checking its AOT/trimming annotations.
