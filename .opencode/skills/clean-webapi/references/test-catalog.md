# Test catalog

What each existing test class protects, so a change can be matched to the tests that should fail and the
suite that proves it. Update this file when a class is added, renamed or removed.

## Unit (`tests/CleanWebApiTemplate.UnitTests`)

| Class | Protects |
|---|---|
| `Api/Common/Abstractions/EndpointRouteBuilderExtensionsTests` | The route-group helpers: `/api/{route}`, tag, 400/500 and 401/403 metadata, policy or plain authentication. |
| `Api/Common/HttpContextCurrentUserTests` | Email claim (`ClaimTypes.Email` and `email`), admin role detection, missing email throws. |
| `Api/Configuration/AppSettingsValidatorTests` | Startup validation of the connection string, issuers and audience. |
| `Api/Configuration/AuthenticationSettingsTests` | Symmetric/Oidc resolution, `local` alias, authority rules, key length, HS256 pinning, `RequireHttpsMetadata`. |
| `Api/Configuration/ConstantsTests` | The four policy names, no duplicates, the configuration key strings. |
| `Api/Configuration/GlobalExceptionHandlerTests` | 4xx `BadHttpRequestException` (incl. 413), invalid JSON, generic 500, no double response. |
| `Api/Configuration/OpenApiSchemaTests` | `CommonOpenApiSchemas` (Ulid, ProblemDetails, sort pair), `TodoOpenApiSchemas`, the helper no-op branches. |
| `Api/Extensions/ApiResultExtensionsTests` | Success shapes (200/201/204, empty array), every error → status, traceId, unknown error throws, missing value 500. |
| `Api/Todo/FilteredTodoRequestTests` | `OwnerScope.FilterOwners`: non-admin pinned, admin keeps the requested owners. |
| `Api/Todo/JsonContractTests` | Every public `*Request`/`*Response` in the feature has source-generated JSON metadata; ProblemDetails too. |
| `Application/Common/Identity/OwnerScopeTests` | `VisibleOwner`, `OwnerForCreate`, `FilterOwners` for admin and non-admin. |
| `Application/Common/Results/ErrorTests` | Error subtypes, titles, defaults. |
| `Application/Common/Results/ResultTests` | Success/failure invariants; `Failure(null)` throws. |
| `Application/Common/Validation/BaseAbstractValidatorTests` | Shared rules: not empty, ULID (single failure), date, range, sort keys, email, failure format. |
| `Application/Common/Validation/ValidatorSetTests` | Empty set refuses to construct; valid input → null; invalid → `BadRequestError`. |
| `Application/Todo/Create/CreateTodoHandlerTests` | Uniqueness pre-check, conflict propagation, trim, DTO mapping, status Pending. |
| `Application/Todo/Create/CreateTodoInputValidatorTests` | Title rules, owner email, description length. |
| `Application/Todo/TodoValidatorTests` | The shared feature rules through a test validator. |
| `Application/Todo/Update/UpdateTodoHandlerTests` | Not found, conflict (pre-check and repository), patch semantics, `UpdatedBy`, no-change path. |
| `Application/Todo/Update/UpdateTodoInputValidatorTests` | Id, title, last writer, description, status values. |
| `Infrastructure/Secrets/OpenBaoClientTests` | Read/write payloads, error payload, token header, address/mount validation. |
| `Infrastructure/Secrets/OpenBaoConfigurationTests` | No-op without address, missing token/path/timeout, provider exposes the entries. |
| `Infrastructure/Todo/TodoSortColumnsTests` | The map matches `TodoSortCatalog.KeySet`. |
| `Infrastructure/Todo/TodoTableTests` | Table/column/index constants and the constraint values. |
| `ServiceDefaults/ExtensionsTests` | `AddServiceDefaults`, `/health` and `/alive`, OTLP exporter only with an endpoint. |

## Component (`tests/CleanWebApiTemplate.Api.ComponentTests`)

| Class | Protects |
|---|---|
| `ApiContractTests` | Unknown route 404 and unsupported method 405 as ProblemDetails, malformed JSON 400, missing/null title, status 0 keeps the stored value, empty filter/titles → `200 []`. |
| `OpenApiContractTests` | Title/version/tag/Bearer, every operation id, documented status codes, schema constraints (max lengths, enum, Ulid, `updatedAt`, owners emails), sort description. |
| `Todo/AuthenticationTests` | Valid token 201, anonymous 401, the eight invalid tokens 401, no-role 403, no-email 403, delete role matrix. |
| `Todo/TodoAuthorizationMatrixTests` | 401/403 for every method of the user group; operator can call every method. |
| `Todo/TodoEndpointsTests` | Create (201, trim, duplicate 409, same title other owner 201, empty/whitespace/long values 400), get 200/404, update 200/404/409, delete 204, filter and titles. |
| `Todo/TodoFilteredEndpointsTests` | SQL paths: ids, title contains (wildcards literal), every criterion, date range and time zone, sort, paging, owner scoping for user and admin. |
| `Todo/TodoOwnershipTests` | Non-admin gets 404/404 for another owner's get/update/delete; admin gets 200/200/204; titles are scoped. |
| `Todo/TodoRepositoryTests` | The real unique index becomes a `ConflictError` result (not an exception). |
| `Todo/TodoValidationTests` | Invalid id 400, unknown delete 404, sort/paging/date/status/id/email/title limits 400. |

## Functional (`tests/CleanWebApiTemplate.Api.FunctionalTests`)

| Class | Protects |
|---|---|
| `Todo/AuthenticationTests` | The out-of-process API trusts the local issuer; valid/anonymous/invalid (7 of 8; `WrongAlgorithm` is component-only) and no-role outcomes; no outbound calls. |
| `Todo/TodoLifecycleFlowTests` | Create → read → update → delete through real HTTP, stored state, no outbound calls. |

## Architecture (`tests/CleanWebApiTemplate.ArchitectureTests`)

| Class | Protects |
|---|---|
| `DependencyRulesTests` | Domain/Application/Infrastructure do not depend on outer layers; Application and Domain do not reference ASP.NET Core; Application does not reference Infrastructure. |
| `StructureTests` | Shared contracts under `Application.Common`; handlers sealed under `Application.{Feature}.{UseCase}`; no Result/Error/Request/Response in Domain; infrastructure feature types are repositories/table/sort maps; ports and routes live in their feature; Application does not reference Infrastructure. |

## Local sidecar (`local/openapi/CleanWebApiTemplate.OpenApiExamples.UnitTests`)

| Class | Protects |
|---|---|
| `LocalAuthTests` | HS256 key length, lifetime range 1–1440, issuer/audience/role claim. |
| `LocalTokenServiceTests` | Token minting, claims, expiry, unknown profile. |
| `LocalUsersTests` | `users.json` shape and unique emails. |
| `OpenApiExamplesMergerTests` | Catalog merge: bodies, parameters (array form), responses, unknown operation, ambiguous parameter, missing content. |
| `SwaggerIndexTests` | The custom UI template placeholder. |

## Which suite for which change

| Change | Run |
|---|---|
| Validator or handler logic | Unit |
| Endpoint, status code, request/response, validation at the edge | Unit + component |
| Ownership, role, policy | Unit (`OwnerScope`) + component (matrix, ownership) |
| SQL, repository, unique index, sort | Component (repository, filtered) + unit (sort alignment) |
| JSON contract, OpenAPI | Unit (`JsonContractTests`, `OpenApiSchemaTests`) + component (`OpenApiContractTests`) |
| Cross-process behaviour, configuration, secrets | Functional |
| Layers, folders, sealed types | Architecture |
| Sidecar, catalog, profiles | `local/openapi/…UnitTests` |
