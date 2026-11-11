# Feature slice: the touch list

A feature in this repository is a **vertical slice**: one business concept crossing Domain, Application,
Infrastructure, API, SQL, local environment and tests. Use this list in order. Skip a step only when the change
genuinely does not need it, and say so in the plan.

## Contents

- [Touch list at a glance](#touch-list-at-a-glance)
- [1. Domain](#1-domain)
- [2. Application](#2-application)
  - [2.1 Input and validator](#21-input-and-validator)
  - [2.2 Port and query filter](#22-port-and-query-filter)
  - [2.3 DTO and mapping](#23-dto-and-mapping)
  - [2.4 Handler](#24-handler)
- [3. Infrastructure](#3-infrastructure)
- [4. Database](#4-database)
- [5. API](#5-api)
- [6. Local environment](#6-local-environment)
- [7. Tests](#7-tests)
- [8. Docs](#8-docs)
- [Adding a new project or database](#adding-a-new-project-or-database)
- [Removing the sample feature](#removing-the-sample-feature)

## Touch list at a glance

| # | Layer | Files |
|---|---|---|
| 1 | Domain | `Models/Entities/{Feature}Entity.cs`, `Models/Enums/{Feature}/E{Feature}Status.cs`, `Models/{Feature}/{Feature}Constraints.cs` |
| 2 | Application | `{Feature}/Handlers/{Verb}/{Verb}{Feature}Input.cs`, `{Verb}{Feature}InputValidator.cs`, `{Verb}{Feature}Handler.cs`; `{Feature}/Handlers/{Feature}Validator.cs`; `Abstractions/Repositories/I{Feature}Repository.cs`; `{Feature}/Handlers/Queries/{Feature}Filter.cs`; `{Feature}/Sorting/{Feature}SortCatalog.cs`; `{Feature}/Projections/{Projection}.cs` when a read selects a subset; `Dtos/{Feature}/{Feature}Dto.cs` + `{Feature}DtoExtensions.cs` |
| 3 | Infrastructure | `Repositories/{Feature}/{Feature}Repository.cs`, `Repositories/{Feature}/Helpers/{Feature}Table.cs`, `Helpers/{Feature}SortColumns.cs` |
| 4 | Database | `local/database/000N__{feature}.sql`, `local/database/seed/000N__{feature}.sql` |
| 5 | API | `Routes/{Feature}/{Verb}/{Verb}{Feature}Request.cs`, `Models/Responses/{Feature}/{Feature}Response.cs`, `Routes/{Feature}/{Feature}Endpoints.cs`, `Routes/{Feature}/{Feature}Routes.cs`; `Configuration/AppJsonSerializerContext.cs`, `Configuration/{Feature}OpenApiSchemas.cs`, `Configuration/OpenApiConfiguration.cs`, `ConfigureServices.cs`, `Extensions/EndpointExtension.cs` |
| 6 | Local | `local/openapi/examples.json`, `bodies/`, `responses/`; `local/tools/devtoken.json` / `local/openapi/users.json` if roles change; `local/aspire/.../AppHost.cs` only if a resource changes |
| 7 | Tests | Unit validator + handler, component endpoints/auth/ownership, OpenAPI contract, functional flow, architecture rules, `Tests.Shared` builder/repository |
| 8 | Docs | `README.md` API table, `tests/README.md` if categories/identities change, `local/local-development.md` if topology/seed/tokens change |

## 1. Domain

Add only what is intrinsic to the concept:

```csharp
// Domain/Models/Entities/ProjectEntity.cs
public sealed class ProjectEntity : BaseEntity<Ulid>
{
    public override Ulid Id { get; set; } = Ulid.NewUlid();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Status { get; set; } = (int)EProjectStatus.Draft;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
```

- Enum values are explicit integers and start at 1 (0 is reserved for "unset" and is rejected by validators).
- Constraints (max lengths, ranges) live in `{Feature}Constraints`, not in SQL or validators. Validators and
  seed data both reference them.
- No `Result<T>`, no HTTP, no configuration, no attributes from ASP.NET.

**Done when:** Domain compiles with `System.*` only; the entity has the audit fields; the enum and constraints
exist.

## 2. Application

### 2.1 Input and validator

- `{Verb}{Feature}Input` is a `record` with `required`/`init` properties. It carries `CreatedBy`/`UpdatedBy`
  when the operation writes audit fields, and `CreatedBy` when the operation is owner-scoped.
- `{Verb}{Feature}InputValidator` derives from the shared `{Feature}Validator<TInput>` and composes
  `BaseAbstractValidator` helpers. It validates **shape**, not uniqueness.
- `{Feature}Validator<TInput>` holds the rules shared by create/update/filter (`ValidateName`,
  `ValidateDescription`, `ValidateStatus`, `ValidateOwnerEmail`).

```csharp
public class CreateProjectInputValidator : ProjectValidator<CreateProjectInput>
{
    public CreateProjectInputValidator()
    {
        RuleFor(x => x.Name)
            .Custom(NotNullNotEmpty)
            .Custom((name, context) => ValidateName(name!, context));

        RuleFor(x => x.CreatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
```

### 2.2 Port and query filter

- `I{Feature}Repository` lives in `Application/Abstractions/Repositories`. It uses only Domain types,
  `Result<T>` and `{Feature}Filter`. Shared service interfaces live next to it in
  `Application/Abstractions/Services`.
- Reads return `Task<T?>`, `Task<IReadOnlyList<T>>` or `Task<bool>`; writes return `Task<Result<T>>` when a
  unique index can fail as a business outcome.
- A read that selects only some fields returns a projection type from
  `Application/{Feature}/Projections` (for example `TodoTitle`), not a partially filled entity or DTO. The
  projection crosses the port, handler and response mapper without `!` casts.
- `{Feature}Filter` is a `record` with `IReadOnlyList<T>?` for lists and `DateTime?` for dates. The API request
  uses `string[]`/`string?`; parsing happens in `ToFilter`.
- `{Feature}SortCatalog` lives in `Application/{Feature}/Sorting`; it owns the accepted sort keys and their
  description. The validators and the OpenAPI schema read it.

### 2.3 DTO and mapping

- `{Feature}Dto` is the full read shape; use it for the ordinary endpoints.
- When the query selects a subset, add a `{Projection}` record in `Application/{Feature}/Projections` (for
  example `TodoTitle`). The port returns it and the response mapper consumes it directly. Do not fill unrelated
  DTO fields and do not narrow the DTO later with `!`.
- `{Feature}DtoExtensions.ToDto` is `internal` and maps entity → DTO in Application.
- The API maps DTO/projection → response; Application never knows the response type.

### 2.4 Handler

- `{Verb}{Feature}Handler` is `sealed`, derives from `ValidationService`, takes the port and a
  `ValidatorSet<TInput>` through its primary constructor, and wraps the action in `ExecuteAsync`.
- It implements `IHandler<{Verb}{Feature}Input, TResult>`, whose contract is
  `Task<Result<TResult>> HandleAsync({Verb}{Feature}Input input, CancellationToken cancellationToken)`.
  The endpoint adapter injects `IHandler<TInput, TResult>`; there is no per-use-case interface.
- `ValidatorSet<TInput>` refuses an empty registration. Register it explicitly and enable `ValidateOnBuild`;
  a missing set is a startup failure, not a silently unchecked input.
- Business rules go here: uniqueness (pre-check + repository `ConflictError`), not-found, state transitions,
  ownership checks that need the stored row.
- Trim/normalise before persistence (`input.Name.Trim()`), set audit fields (`DateTime.UtcNow`), and return
  `Result<T>.Failure(new ConflictError(...))` rather than throwing.
- If a second handler needs the same behaviour and it is not a repository port, extract a shared service
  interface to `Application/Abstractions/Services` and implement it next to the feature. The handler still
  owns the use case; the service is only the shared collaborator.

```csharp
public sealed class CreateProjectHandler(IProjectRepository repository,
                                        ValidatorSet<CreateProjectInput> validators)
    : ValidationService, IHandler<CreateProjectInput, ProjectDto?>
{
    public Task<Result<ProjectDto?>> HandleAsync(CreateProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            ProjectEntity entity = input.ToEntity();
            DateTime now = DateTime.UtcNow;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.Status = (int)EProjectStatus.Draft;

            if (await repository.NameExistsAsync(entity.Name, entity.CreatedBy, cancellationToken: cancellationToken))
            {
                return Result<ProjectDto?>.Failure(
                    new ConflictError("A project with this name already exists for this owner."));
            }

            Result<ProjectEntity> insertion = await repository.InsertAsync(entity, cancellationToken);
            return insertion.IsFailure
                ? Result<ProjectDto?>.Failure(insertion.Error!)
                : Result<ProjectDto?>.Success(insertion.Value!.ToDto());
        }, cancellationToken);
}
```

**Done when:** the handler compiles without ASP.NET/Npgsql references; every failure is a typed `Error`; the
port is the only persistence surface; the validator has no database access.

## 3. Infrastructure

- `{Feature}Table` holds the table and column constants and the unique index name. It is the single source of
  truth for SQL text; the DDL stays in `local/database`.
- `{Feature}SortCatalog` (Application) owns the accepted sort keys; `{Feature}SortColumns` maps those keys to
  **column names**. Only keys in the map can reach `ORDER BY`. Add a unit test that asserts
  `{Feature}SortCatalog.KeySet.SetEquals({Feature}SortColumns.Map.Keys)`; the two lists must not drift.
- `{Feature}Repository` implements the port with `INpgsqlConnectionFactory`. Every query uses bound
  parameters; every column name comes from `{Feature}Table`; every row is mapped explicitly.
- `Ulid` maps to `bytea` (`id.ToByteArray()` / `new Ulid(bytes)`); `DateTime` maps to `timestamptz` in UTC;
  `string` maps to `text`; the status maps to `integer`.
- A unique violation is caught with
  `PostgresException when exception.SqlState == PostgresErrorCodes.UniqueViolation` and returned as
  `ConflictError`; do not let it become a 500.

**Done when:** every value is a parameter; the sort catalog and the column map agree, enforced by a test; the
repository maps every column with `IsDBNull` checks; no business rule lives here beyond translating a constraint
violation.

## 4. Database

- Add `local/database/000N__{feature}.sql` with the table, primary key, unique index and the indexes the
  queries need. `db-init` applies all `*.sql` files in ordinal order, each in a transaction, on every start.
- Add `local/database/seed/000N__{feature}.sql` with rows that make every endpoint usable locally. The seed is
  applied only in Local mode; tests get an empty schema.
- Seed rows use the same emails/roles as `local/tools/devtoken.json` and `local/openapi/users.json`
  (`local.user@example.com`, `local.operator@example.com`, `local.admin@example.com`).
- If you remove or rename the sample table, update the readiness query in
  `local/aspire/{App}.DbInitializer/Program.cs` (it currently checks `"Todo"`).
- A new table in the same database needs **no** new resetter name: Respawn resets all tables. A new **database**
  needs a name in `DatabaseResetConfiguration.Names` and a resetter registered by the fixture.

**Done when:** the schema applies from an empty database, the seed covers every state the endpoints accept,
and the `db-init` job exits 0.

## 5. API

- `Routes/{Feature}/{Verb}/{Verb}{Feature}Request.cs`: request contract, `ToInput(OwnerScope owner)` mapping.
  The owner comes from `currentUser.Owner`, never from the body: create uses `OwnerForCreate`, reads use
  `VisibleOwner`, filter uses `FilterOwners`.
- `Models/Responses/{Feature}/{Feature}Response.cs`: response contract implementing
  `IBaseResponse<{Feature}Dto?, {Feature}Response>`; `ToResponseModel` maps the DTO. A projection response
  implements `IBaseResponse<{Projection}, {Projection}Response>`.
- `Routes/{Feature}/{Feature}Endpoints.cs`: the endpoint adapter. Each method takes `HttpContext`, `ICurrentUser`,
  the handler and `CancellationToken`, maps the request to the input, calls the handler and returns
  `result.ToResponse<...>(context)` or `result.ToCreatedResponse<...>(context, ...)`.
- `Routes/{Feature}/{Feature}Routes.cs`: `sealed class {Feature}Routes : IGroupMap` (no `ICurrentUser`
  constructor). Create the groups with `CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.X_POLICY])`, map
  the adapter methods, add `WithName`/`WithSummary`/`WithDescription` and `Produces`/`ProducesProblem`.
- `ConfigureServices.cs`: register the validator, the `ValidatorSet<TInput>`, the handler and the repository
  explicitly. No scanning. Handlers are registered by their closed `IHandler<TInput, TResult>` interface.
- `Program.cs`: keep `UseDefaultServiceProvider(options => options.ValidateOnBuild = true)` so a missing
  `ValidatorSet<T>` fails at startup.
- `EndpointExtension.cs`: `services.TryAddEnumerable(ServiceDescriptor.Transient<IGroupMap, {Feature}Routes>());`.
- `AppJsonSerializerContext.cs`: add every request, response and collection type (`JsonSerializable`), plus
  any new error/ProblemDetails type.
- `OpenApiConfiguration.cs`: add the tag and call `{Feature}OpenApiSchemas.DescribeSchemas(document)`.
- `{Feature}OpenApiSchemas.cs`: patch schema properties with descriptions, formats, max lengths, enums and
  examples. Use `nameof({Verb}{Feature}Request)` / `nameof({Feature}Response)` for schema keys and
  `{Feature}Constraints` for lengths. Use `OpenApiSchemaHelpers` (`Describe`, `Property`, `ArrayItems`,
  `DescribePaging`, `DescribeSort`, `DescribeStatus`). `DescribeSort` writes the Todo catalog and
  `DescribeStatus` derives from `ETodoStatus`; for another feature override the sort description with
  `{Feature}SortCatalog.Describe()` and, if its status values differ, set `schema.Enum` from its enum.

**Done when:** every route has a policy, a name, a summary, a description and the documented status codes;
every contract is in the JSON context; the OpenAPI document contains the new schemas and operations; the
endpoint adapter is the only place that maps `Result` to `IResult`.

## 6. Local environment

- Add examples to `local/openapi/examples.json` for the new operations, with bodies under
  `local/openapi/bodies/` and responses under `local/openapi/responses/`. Keep the example values consistent
  with the seed (same ids, emails and states).
- If the feature adds a role, add a profile to `local/tools/devtoken.json` and a user to
  `local/openapi/users.json`; keep the emails and roles identical.
- A new table normally needs no AppHost change. A new resource, container or secret does: update
  `AppHost.cs`, `AppHostContract.cs`, the initializer and `local/local-development.md`.
- If the feature changes the local JWT, OpenBao or database topology, run `aspire start` and verify the
  resource graph and the seed.

**Done when:** the Swagger profile picker can call every new operation with seeded data, and a fresh
`aspire start` rebuilds schema + seed without manual SQL.

## 7. Tests

Write the smallest set that proves the behaviour at the right level. A feature normally needs:

| Test | Project | Proves |
|---|---|---|
| Input validator | `Api.UnitTests` | Shape rules, null/empty/length/email/ULID/sort/paging; no Docker |
| Validation set | `Api.UnitTests` | `ValidatorSet<T>` rejects an empty set; validators aggregate their failures |
| Handler | `Api.UnitTests` | Business rules with a substituted port: uniqueness, not-found, propagation of `ConflictError`, trimming, audit fields. Build the handler with a real `ValidatorSet` (or a test validator), not `[]` |
| Owner scope | `Api.UnitTests` | `VisibleOwner`, `OwnerForCreate`, `FilterOwners` for admins and non-admins |
| Sort alignment | `Api.UnitTests` | `{Feature}SortCatalog.KeySet` equals `{Feature}SortColumns.Map.Keys` |
| Endpoints | `Api.ComponentTests` | HTTP contract, validation → 400, duplicate → 409, missing → 204/404, persistence, ownership, ProblemDetails |
| Authorization matrix | `Api.ComponentTests` | 401 anonymous, 403 without role/wrong role, 200/201 for allowed roles, on every new protected route |
| Ownership | `Api.ComponentTests` | Non-admin cannot read/update/delete another owner's row; admin can |
| Flow | `Api.FunctionalTests` | Create → read → filter/update/delete through real HTTP as a separate process |
| Architecture | `ArchitectureTests` | New ports/filters live in Application, repositories only under `Repositories`, no outer-layer dependency. If the feature adds repository helpers, generalize the hardcoded `Todo.Helpers` condition in `StructureTests` to `.Contains(".Helpers")`. |
| OpenAPI contract | `Api.ComponentTests` | New operation ids, tag, status codes and schema constraints appear in `/openapi/v1.json`. `OpenApiContractTests` asserts the exact operation-id set, so a new endpoint fails it until the ids/paths are added. |

Also add to `Tests.Shared`:

- `{Feature}Builder` with valid defaults and a `CreatedBy`/status method.
- `{Feature}TestRepository` over the real repository, with `AddAsync`, `FindAsync`, `FindByNameAsync`,
  `CountAsync` and `TryAddAsync` for conflict assertions.
- `{Feature}SortColumnsTests` (or the existing helper test) asserting the catalog and the SQL map agree.
- A `Projects` property on `TestServerFixtureBase` initialized with the connection string, and its use in the
  component tests (`Fixture.Projects`).

**Done when:** each new behaviour has a test that fails without the change; the auth matrix covers every new
route; the tests use attributes/builders/resets and no sleeps or hardcoded ids.

## 8. Docs

- `README.md`: add the endpoint to the API table (method, route, policy, success, errors) and any new
  configuration/secret row.
- `tests/README.md`: update only if the test categories, identities or coverage thresholds change.
- `local/local-development.md`: update only if the AppHost topology, seed, OpenBao path or devtoken profiles
  change.
- `AGENTS.md`: update only if the AOT or hook rules change.

## Adding a new project or database

**New project** (a worker, a second API, a shared library):

1. Create `src/{App}.<Role>/{App}.<Role>.csproj` (or `local/` for a local-only tool). Inherit
   `Directory.Build.props`; add package versions to `Directory.Packages.props`, not to the `.csproj`.
2. Add only references that follow the dependency rule: a runtime project may reference Application/Domain;
   Application and Domain must not reference the new project unless it is an inner layer.
3. Add it to the solution in the existing solution folder (`dotnet sln add`).
4. Add its assembly to `DependencyRulesTests`' `ArchLoader` and a rule for its allowed dependencies. Add a
   `StructureTests` assertion if it introduces a new convention.
5. If it runs under the AppHost, add a resource, a name in `AppHostContract.Resources`, a trust mapping if it
   authenticates, and a test fixture/wait. If it is a test-only project, add it to the test project references
   and the test README.
6. Update `README.template.md` architecture and commands.

**New database**:

1. Add the resource and database in the AppHost, with a `WithReference` name that matches the key the
   application reads (`ConnectionStrings:{Name}`).
2. Add a connection-string section to `AppSettings` (or reuse the existing one) and a second
   `INpgsqlConnectionFactory`/repository registration.
3. Add schema scripts under a separate folder or prefix, and a resetter name in `DatabaseResetConfiguration`.
4. Add the schema initializer step if the database is not on the AppHost's existing `db-init` path.
5. Add a component test that resets and queries the new database; add the connection string to the fixture.

## Removing the sample feature

When the user wants the template's `Todo` sample removed, do it in dependency order and keep the solution
green at each step:

1. Find every reference:

   ```bash
   rg -n "Todo|todo" src tests local README.md README.template.md AGENTS.md \
     --glob '!**/bin/**' --glob '!**/obj/**'
   ```

2. Remove the API surface: `Routes/Todo`, `Models/Responses/Todo`, the `Todo` tag and descriptor call in
   `OpenApiConfiguration`, the `TodoRoutes` registration in `EndpointExtension`, the Todo request/response
   entries in `AppJsonSerializerContext`, and the Todo validator/handler/repository registrations in
   `ConfigureServices`.
3. Remove Application: `Application/Todo/**`, `Application/Dtos/Todo/**`.
4. Remove Domain: `Domain/Models/Entities/TodoEntity.cs`, `Domain/Models/Enums/Todo/**`,
   `Domain/Models/Todo/**`.
5. Remove Infrastructure: `Infrastructure/Repositories/Todo/**`.
6. Remove SQL: `local/database/0001__initial_schema.sql` (or replace it with the new first migration) and
   `local/database/seed/0001__todos.sql`.
7. Update `local/aspire/{App}.DbInitializer/Program.cs`: the readiness query must read a table that still
   exists (for example the new feature's table), otherwise `db-init` exits 1 and the API never starts.
8. Remove tests: `Api.UnitTests/**/Todo*`, `Api.ComponentTests/Todo/**`,
   `Api.FunctionalTests/Todo/**`; update `Tests.Shared/Data/TodoBuilder.cs`,
   `TodoTestRepository.cs`, `TestUser` members that only exist for Todo, the `Todos` property on the fixture
   and `DatabaseResetConfiguration` names if they are Todo-specific.
9. Remove local examples: the Todo operations in `local/openapi/examples.json`, `bodies/`, `responses/` and
   the Todo users/profiles only if no other feature uses them.
10. Run the gates. A green build is not enough: run the AOT publish and the full test suite.

If the user is replacing Todo with a new feature, create the new table and seed **before** removing the
initializer's readiness query, or add an intermediate query that works for both.

**Done when:** `rg` finds no `Todo` in `src/`, `tests/` or `local/` (except deliberately kept docs), the
initializer exits 0, and build, AOT publish and every test pass.
