# API

The Api project is the composition root and the only project that knows HTTP. This reference covers the
registration model, the route groups, the Result→HTTP mapping, ProblemDetails, JSON and OpenAPI.

## Composition

`Program.Main`:

1. `WebApplication.CreateSlimBuilder(args)`.
2. `UseDefaultServiceProvider(ValidateScopes = true, ValidateOnBuild = true)`.
3. `UseKestrelHttpsConfiguration()` — the slim builder does not wire HTTPS by itself.
4. `AddServiceDefaults()` (OpenTelemetry, `/health`, `/alive`).
5. `builder.LoadAppSettings(args)` — configuration sources and startup validation.
6. `builder.Services.AddApiServices(builder.Configuration, appSettings)`.
7. `builder.Build()`.
8. `app.Services.ValidateUseCases()` — builds every use case once.
9. `app.ConfigureApiApplication(appSettings)` and `app.MapDefaultEndpoints()`.

`ConfigureServices.AddApiServices` registers the PostgreSQL data source, the feature modules, health checks,
CORS, the exception handler, ProblemDetails, compression, authentication/authorization, OpenAPI (only when
enabled), `ICurrentUser`, the shared JSON context and the HTTP JSON options.

```csharp
services.AddPostgres(appSettings.ConnectionStrings.Postgres);   // singleton NpgsqlDataSource
services.AddTodoModule();                                        // one per feature
```

### Modules

`Api/{Feature}/{Feature}Module.cs` owns everything the feature needs:

```csharp
internal static class TodoModule
{
    public static IServiceCollection AddTodoModule(this IServiceCollection services)
    {
        services.AddScoped<ITodoRepository, TodoRepository>();

        services.AddUseCase<CreateTodoInput, TodoDto, CreateTodoHandler, CreateTodoInputValidator>();
        // … one per use case

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGroupMap, TodoRoutes>());

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonSerializerContext.Default));

        services.Configure<OpenApiOptions>(OpenApiConfiguration.DocumentName, options =>
            options.AddDocumentTransformer((document, _, _) =>
            {
                TodoOpenApiSchemas.DescribeSchemas(document);
                return Task.CompletedTask;
            }));

        return services;
    }
}
```

### AddUseCase

`Api/Common/Composition/UseCaseRegistrationExtensions.cs`:

```csharp
services.AddUseCase<TInput, TResult, THandler, TValidator>();
```

registers:

- `IValidator<TInput>` → `TValidator`
- `ValidatorSet<TInput>` (constructed from every registered `IValidator<TInput>`)
- `IHandler<TInput, TResult>` → `THandler`
- an `IStartupCheck` that resolves the set and the handler once.

`ValidateUseCases()` creates a scope and runs every check. `ValidateOnBuild` alone does **not** run
constructors: it only proves that registered services can be built. The startup check is what makes an empty
validator set (or a missing registration) stop the host instead of failing on the first request.

## Route groups

`IGroupMap` is the only contract:

```csharp
public interface IGroupMap
{
    void MapGroup(IEndpointRouteBuilder app);
}
```

`EndpointExtension.MapRoutes` resolves every registered `IGroupMap` and calls it. Modules register them with
`TryAddEnumerable`, so a feature can add one or several groups.

The helpers in `Api/Common/Abstractions/EndpointRouteBuilderExtensions.cs`:

```csharp
app.CreateBaseRouteGroupBuilder("todos", "Todos");                        // /api/todos + tag + 400/500 problems
app.CreateAuthorizedRouteGroupBuilder("todos", "Todos", [Constants.UserPolicy]); // + 401/403 + policy
```

A feature normally has two groups over the same prefix: one with `User` (read, create, update) and one with
`Operator` (delete). Policies are cumulative: `User` accepts User, Operator and Admin; `Operator` accepts
Operator and Admin; `Admin` only Admin; `External` accepts External, Operator and Admin.

## Endpoint adapter

`{Feature}Endpoints` is a static class with one method per operation. It maps the request, calls the handler
and converts the result; it never touches a repository.

```csharp
public static async Task<IResult> GetByIdAsync(string id,
                                               HttpContext context,
                                               ICurrentUser currentUser,
                                               IHandler<GetTodoByIdInput, TodoDto> handler,
                                               CancellationToken cancellationToken)
{
    GetTodoByIdInput input = new() { Id = id, VisibleOwner = currentUser.Owner.VisibleOwner };
    Result<TodoDto> result = await handler.HandleAsync(input, cancellationToken);

    return result.ToResponse<TodoDto, TodoResponse>(context);
}
```

`{Feature}Routes` keeps the templates, policies and documentation:

```csharp
userGroup.MapGet("/{id}", TodoEndpoints.GetByIdAsync)
    .WithName("GetTodoById")
    .WithSummary("Get a Todo by id")
    .WithDescription("… 404 when the id does not exist or is not visible …")
    .Produces<TodoResponse>((int)HttpStatusCode.OK)
    .ProducesProblem((int)HttpStatusCode.NotFound);
```

Route patterns: `POST /` for create, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `POST /filter`,
`POST /titles`. The group prefix is `/api/{route}`.

## Result to HTTP

`ApiResultExtensions` is the only mapper:

| Method | Success | Failure |
|---|---|---|
| `ToResponse<TDto, TResponse>` | `200` with `TResponse.ToResponseModel(dto)` | `ResolveError` |
| `ToResponse<TDto, TResponse>` on `Result<IEnumerable<TDto>>` | `200` with an array (empty array when nothing matched) | `ResolveError` |
| `ToCreatedResponse<TDto, TResponse>` | `201` + `Location` | `ResolveError` |
| `ToNoContentResponse<T>` | `204` | `ResolveError` |

`ResolveError` maps `BadRequestError`/`DomainError` → 400, `NotFoundError` → 404, `ConflictError` → 409,
`UnauthorizedError` → 401, `ForbiddenError` → 403 and throws `ArgumentOutOfRangeException` for an unknown
subtype (the exception handler turns it into a 500).

A success with a null value is a bug in the use case: `ApiResultExtensions` returns a `500` ProblemDetails
("The use case returned no value.") instead of serializing `null` or throwing a `NullReferenceException`.

## ProblemDetails

- `ProblemDetailsFactory.Create(context, status, detail, title)` builds RFC 9457 bodies with
  `type = https://httpstatuses.io/{status}` and `traceId`.
- `ProblemDetailsFactory.WriteAsync` writes `application/problem+json; charset=utf-8` using the
  source-generated `ProblemDetails` metadata.
- `GlobalExceptionHandler` handles `BadHttpRequestException` (keeps its 4xx status, 413 for a body too large),
  `JsonException` (400) and every other exception (500). It logs with `[LoggerMessage]` and does not leak the
  exception message to the client.
- `UseStatusCodePages` gives routing, 404, 405 and body-less status codes the same contract.

## Requests, responses and JSON

- Requests are `sealed record` with `required` for mandatory members. An omitted optional string is `null` on
  the wire; the `ToInput` mapping turns it into `string.Empty` (`Description ?? string.Empty`). Do not rely on
  a property initializer for a missing JSON property: the source-generated serializer leaves it `null`.
- Responses are `sealed record` with `required`/`init`, non-nullable, and map explicitly from the DTO.
- Every request, response and collection type goes in the feature's `{Feature}JsonSerializerContext`:

  ```csharp
  [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
  [JsonSerializable(typeof(CreateTodoRequest))]
  [JsonSerializable(typeof(IEnumerable<TodoResponse>))]
  internal sealed partial class TodoJsonSerializerContext : JsonSerializerContext;
  ```

- `AppJsonSerializerContext` holds `ProblemDetails`. The chain is per-feature context first, then the shared
  one:

  ```csharp
  services.ConfigureHttpJsonOptions(options =>
      options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
  ```

- `JsonContractTests` (unit) asserts every public `*Request`/`*Response` in the feature namespace has metadata
  in the context. A missing entry fails that test; in the AOT binary it would fail at runtime.

## OpenAPI

`OpenApiConfiguration` (`Api/Configuration`) registers the single document (`v1`):

- `info` title `CleanWebApiTemplate API`, version `1.0.0`, generic description;
- the `Bearer` security scheme and a global security requirement;
- `CommonOpenApiSchemas`: `Ulid` (format/pattern/example), `ProblemDetails` field descriptions and the
  `KeyValuePairOfstringAndboolean` sort pair (generic text).

The document is served at `/openapi/v1.json` only when `OpenApi__Enabled` is `true` (the base
`appsettings.json` enables it; `Production` disables it).

Each feature adds a document transformer from its module and describes its own schemas:

```csharp
services.Configure<OpenApiOptions>(OpenApiConfiguration.DocumentName, options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        TodoOpenApiSchemas.DescribeSchemas(document);
        return Task.CompletedTask;
    }));
```

`OpenApiSchemaHelpers`:

| Helper | Use |
|---|---|
| `Patch(schemas, name, describe)` | Run `describe` only when the schema exists |
| `Property(schema, "name")` / `ArrayItems(schema)` | Navigate the generated schema |
| `Describe(schema, description, format, maxLength, example)` | Set the common metadata |
| `DescribePaging(schema)` | `pageNumber`/`pageSize`: minimum 1, examples |
| `DescribeSort(schema, keys)` | Sort description with the feature's catalog keys and an example |
| `DescribeStatus<TEnum>(schema, description, withAllowedValues)` | Status names, example and allowed values |

Add the feature tag in `{Feature}OpenApiSchemas.DescribeSchemas` (`document.Tags.Add(new OpenApiTag { … })`).
Do not describe Todo keys from shared code: each feature passes its own.

## Status codes

| Operation | Success | Errors |
|---|---|---|
| `GET /{id}` | `200` | `400` invalid id, `401`, `403`, `404` missing or invisible, `500` |
| `POST /filter` | `200` (empty array when nothing matches) | `400`, `401`, `403`, `500` |
| `POST /titles` | `200` (empty array) | `400`, `401`, `403`, `500` |
| `POST /` | `201` + `Location` | `400`, `401`, `403`, `409` duplicate, `500` |
| `PUT /{id}` | `200` | `400`, `401`, `403`, `404`, `409`, `500` |
| `DELETE /{id}` | `204` | `400`, `401`, `403`, `404`, `500` |

The group already documents `400`, `401`, `403` and `500`; the operation adds the rest.

## Host plumbing

- **CORS**: one policy (`DefaultCorsPolicyName`), origins and credentials from configuration; an empty origin
  list allows nobody.
- **Compression**: gzip for HTTPS and `application/json`.
- **Health**: `/health` (aggregate), `/alive`, `/health/api`, `/health/postgresDb`.
- **Exception handler**: registered with `AddExceptionHandler<GlobalExceptionHandler>()` and used first in the
  pipeline.

## Adding a contract type

1. Add the request/response in the feature folder.
2. Add `[JsonSerializable(typeof(T))]` to the feature context (and the collection type if it is returned).
3. Describe it in `{Feature}OpenApiSchemas` (and `CommonOpenApiSchemas` only if it is shared).
4. Add the operation metadata in `{Feature}Routes`.
5. Add the unit `JsonContractTests` coverage (automatic if the type follows the suffix convention) and the
   component/OpenAPI assertions.
6. Add the local Swagger example.
