# Test catalog

What each test class in the template protects, so an agent can pick the right suite to run and know which
tests to mirror for a new feature. The `Todo` rows are the sample; they disappear with the sample, but the
category and the shared infrastructure stay.

## Unit tests — `{App}.Api.UnitTests`

No Docker, no host. Run this project first; it is the fastest signal.

### Api / Common

| Class | What it proves |
|---|---|
| `EndpointRouteBuilderExtensionsTests` | The base group maps `/api/{name}` and documents 400/500; the authorized group adds the policy, 401/403 metadata, and requires authentication without a policy. |
| `HttpContextCurrentUserTests` | `Email` reads the mapped and short email claims and throws without one; `Owner` builds an `OwnerScope` whose `VisibleOwner` is null for Admin and the email for everyone else. |
| `OwnerScopeTests` | `VisibleOwner`, `OwnerForCreate` and `FilterOwners` for admins and non-admins. |

### Api / Configuration

| Class | What it proves |
|---|---|
| `AppSettingsValidatorTests` | Required connection string, at least one issuer (HTTPS or URN), no empty issuer, required audience; every failure is reported. |
| `AuthenticationSettingsTests` | Symmetric vs Oidc inference, authority/mode combinations, key length, missing key, invalid authority, invalid mode, ignored `API_KEY` in Oidc. |
| `ConstantsTests` | Every policy is listed once, the JWT scheme is listed, the configuration keys keep their expected values. |
| `GlobalExceptionHandlerTests` | Null exception, 500 ProblemDetails, BadHttpRequestException, JsonException, null logger. |
| `OpenApiSchemaTests` | Schema descriptors are no-ops without components/properties, describe Ulid/ProblemDetails/sort, derive status values from `ETodoStatus`, and patch only existing schemas. |

### Api / Extensions and Routes

| Class | What it proves |
|---|---|
| `ApiResultExtensionsTests` | 200/201/204/collection/empty collection/scalar/empty string; each `Error` maps to its ProblemDetails/401/403; `traceId` is present; an unhandled error type throws. |
| `FilteredTodoRequestTests` | `ToInput(OwnerScope)` forces the authenticated email for non-admins and keeps the requested owners for admins. |

### Application

| Class | What it proves |
|---|---|
| `BaseAbstractValidatorTests` | `NotNullNotEmpty`, `ValidateUlid`, `ValidateDateTime`, date-range pairing/order, sort-key duplicates/unknown/case-insensitive, email parsing, explicit property name. |
| `ErrorTests` | Each error record exposes the default or supplied title/description; 401/403 have none. |
| `ResultTests` | Success/failure invariants: success with value/null, failure with error, invalid constructor states throw. |
| `CreateTodoHandlerTests` | Duplicate title per owner → `ConflictError` without inserting; same title for another owner inserts; repository conflict propagates; unique title returns the trimmed DTO. |
| `UpdateTodoHandlerTests` | Unknown id → `NotFoundError`; duplicate title → `ConflictError` without updating; repository conflict propagates; row disappears before update → `NotFoundError`; valid changes return the DTO. |
| `TodoValidatorTests` | Shared title/description/owner/status rules, including max lengths and registered enum values. |
| `ValidatorSetTests` | An empty `ValidatorSet<T>` throws; a valid input passes; a failing validator returns `BadRequestError`. |
| `CreateTodoInputValidatorTests` | Create input: valid, empty/whitespace/null title, overlong title, invalid email. |
| `UpdateTodoInputValidatorTests` | Update input: valid no-op, invalid ULID, whitespace/overlong title, unregistered status. |

### Infrastructure and ServiceDefaults

| Class | What it proves |
|---|---|
| `TodoTableTests` | Table/column constants and constraints stay in sync. |
| `TodoSortColumnsTests` | `TodoSortCatalog.KeySet` equals `TodoSortColumns.Map.Keys`; every catalog key resolves case-insensitively. |
| `OpenBaoClientTests` | KV v2 read/write, token header, path, error messages never include the token, unexpected payloads throw, invalid options throw. |
| `OpenBaoConfigurationTests` | No address → no-op; missing setting/invalid timeout → throw; complete config adds one source; every entry becomes a configuration key; server failure throws. |
| `ExtensionsTests` | OTLP exporter only with an endpoint; `/health` and `/alive` are mapped in and out of Development. |

## Component tests — `{App}.Api.ComponentTests`

Docker required. The shared AppHost starts infrastructure and schema; the API runs in-process.

| Class | What it proves |
|---|---|
| `ApiContractTests` | Unknown route/method → ProblemDetails; malformed JSON, missing/null title, status 0, no-match filter/titles → the documented 400/204 contract. |
| `OpenApiContractTests` | Document title/version/tag/Bearer; the exact operation-id set; every operation's status codes; schema constraints (ULID pattern, max lengths, enum, ProblemDetails). Add the new feature's ids, paths, tag and schemas in the same change: the exact-set assertion fails as soon as a new endpoint appears. |
| `Todo/AuthenticationTests` | Valid user token → 201; no token/invalid token kinds → 401; no roles/no email → 403; delete role matrix (User 403, Operator/Admin 200, External 403). |
| `Todo/TodoAuthorizationMatrixTests` | The user group (GET/filter/titles/update) is 401 anonymous, 403 without roles/External, 200 as Operator. |
| `Todo/TodoEndpointsTests` | Create (owner/status/location/trim/empty/duplicate/overlong), get (200/204), update (patch, unknown, duplicate, other owner), delete, filter, titles projection. |
| `Todo/TodoFilteredEndpointsTests` | Ids, title contains, wildcard escaping, every criterion, date range/offset, sort, pagination, admin vs user owner filters, one date bound, unknown sort, incomplete paging. |
| `Todo/TodoOwnershipTests` | User cannot read/update/delete another owner's row; admin can; titles are owner-scoped or global for admins. |
| `Todo/TodoRepositoryTests` | The unique index maps to `ConflictError` (not a 500) when the pre-check is bypassed. |
| `Todo/TodoValidationTests` | Invalid ULID, unknown id delete → 200 false, invalid sort, paging, date range, status, owner email, overlong title fragment, empty title update, null body. |

`ComponentTestFixture` starts the AppHost in `Component` mode and asserts the app reads the test configuration,
the secret came from OpenBao, and no JwtBearer scheme downloads metadata. `ComponentTestBase` adds
`CreateClientWithServices` for a second application instance. `BlockExternalHttpHandler` refuses non-local HTTP
calls.

## Functional tests — `{App}.Api.FunctionalTests`

Docker required. The app runs as a separate process under the AppHost; the test only uses HTTP.

| Class | What it proves |
|---|---|
| `Todo/AuthenticationTests` | A valid local token creates a Todo without any outbound call; no token → 401; every invalid token kind → 401; no roles → 403. |
| `Todo/TodoLifecycleFlowTests` | Create → read → update → delete through real HTTP, with the stored state checked at each step. |

`FunctionalTestFixture` starts the AppHost in `Functional` mode, starts the API explicitly, waits for
`/health/api`, and exposes `OutboundCalls` (assert it stays empty).

## Architecture tests — `{App}.ArchitectureTests`

No Docker.

| Class | What it proves |
|---|---|
| `DependencyRulesTests` | Domain does not depend on outer layers; Application does not depend on Infrastructure/Api/ServiceDefaults; Infrastructure does not depend on Api/ServiceDefaults; Application/Domain assemblies do not reference ASP.NET Core; Application does not reference Infrastructure. |
| `StructureTests` | `I{Feature}Repository` and `{Feature}Filter` live in Application; `Result<T>` lives in `Application.Results`; classes under `Infrastructure.Repositories` are repositories or live under a `Helpers` namespace. |

**Known limitation:** the helper condition hardcodes `Repositories.Todo.Helpers`; generalize it to
`.Contains(".Helpers")` when a second feature adds helpers. See `ProjectArchitectureTests.md`.

## Local OpenAPI sidecar tests — `local/openapi/{App}.OpenApiExamples.UnitTests`

These test the local-only sidecar, not `src/`. They run without Docker.

| Class | What it proves |
|---|---|
| `LocalAuthTests` | Sidecar auth settings require a long key, a valid issuer/audience/role claim and a positive lifetime. |
| `LocalTokenServiceTests` | The sidecar mints the same HS256 claims/signature as devtoken; a no-role profile omits the role claim. |
| `LocalUsersTests` | Profiles/roles load; a missing email or file is rejected. |
| `OpenApiExamplesMergerTests` | Examples merge by operation/parameter/response, route constraints/trailing slashes, inline values, existing examples kept, unknown operation/parameter/response reported, missing body, body+value conflict, parameter refs, path vs operation parameter precedence, Bearer scheme/server, ProblemDetails examples. |
| `SwaggerIndexTests` | The UI template embeds the users; a missing template is rejected. |

## Shared infrastructure — `{App}.Tests.Shared`

Not tests: the base classes and helpers the other projects use. `TestBase` applies the reset, seeds and builds
`Client`; `TestServerFixtureBase` owns the AppHost and readiness; `TestUser`/`TestUsers`/`TestTokenIssuer`
provide identities; `TestData`/`{Feature}Builder` provide deterministic data; `{Feature}TestRepository` reads
the database; `DatabaseResetConfiguration`/`RespawnDatabaseResetter` reset it; `Eventually` waits;
`OutboundCallRecorder` catches external calls.

## Which suite for which change

| Change | Minimum suites |
|---|---|
| Validator, `ValidatorSet` or pure helper | Unit |
| Handler business rule | Unit + component (the HTTP/database path) |
| Owner-scope rule | Unit (`OwnerScopeTests`) + component (ownership) |
| Sort key or column map | Unit (`*SortColumnsTests`) + component (filtered sort) |
| Endpoint contract or status code | Component + OpenAPI contract |
| SQL, index, filter, sort, pagination | Component (repository/filtered tests) |
| JWT/policy/ownership | Component (authentication/authorization/ownership) + functional (at least the 401/403/valid flow) |
| Cross-endpoint journey | Functional |
| New project/namespace | Architecture + unit/component for the new code |
| Local sidecar/examples | `local/openapi` unit tests + a local `aspire start` check |

When the sample `Todo` is removed, delete its rows from this catalog and add the replacement feature's rows.
Do not delete the categories: the shared fixtures, the OpenAPI contract tests and the architecture tests still
apply.
