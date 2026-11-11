# Feature slice

The ordered touch list for a new feature, one endpoint, a new column and the removal of the sample. Follow it
top to bottom: each phase leaves the solution compiling and the previous phase is the input of the next one.

Use `Todo` as the live example and `assets/examples/` for a second complete feature (`Project`).

## 1. Plan (before editing)

Write down:

- the routes, verbs, policies and the success/error status of each operation;
- every type per layer and the file it goes in;
- the schema and seed changes;
- the registrations (the module call and what the module registers);
- the JSON context and OpenAPI entries;
- the tests per project, naming the behaviour each one proves;
- the local examples and docs.

Wait for confirmation when the change adds a table, a secret, a policy, a project or changes the HTTP
contract.

## 2. Domain

`src/CleanWebApiTemplate.Domain/{Feature}/`

1. `{Feature}Entity.cs` — `sealed class : BaseEntity<Ulid>`; `required` members for what must be set; defaults
   for the rest; audit columns `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`.
2. `E{Feature}Status.cs` — explicit values from 1.
3. `{Feature}Constraints.cs` — lengths and limits the validators and OpenAPI descriptors read.

Never put a `Result`, an HTTP concept or a SQL type in Domain.

## 3. Application

`src/CleanWebApiTemplate.Application/{Feature}/`

1. `I{Feature}Repository.cs` — one method per query. Every read that can be scoped to an owner takes
   `string? visibleOwner = null`; every write that can be scoped takes the same. Inserts and updates return
   `Result<T>` when they can conflict.
2. `{Feature}Dto.cs` — the read model. `required`/`init`, non-nullable where the entity guarantees a value.
3. `{Feature}DtoExtensions.cs` — `internal static {Feature}Dto ToDto(this {Feature}Entity entity)`.
4. `{Feature}Filter.cs` — the persistence-neutral filter (`Ids`, `Titles`, `Statuses`, `Owners`, dates,
   paging, sort).
5. `{Feature}Title.cs` — the projection, if the feature has a partial read.
6. `{Feature}SortCatalog.cs` — the accepted keys, `KeySet` and `Describe()`.
7. `{Feature}Validator.cs` — `abstract class : BaseAbstractValidator<T>` with the shared rules
   (`ValidateTitleLength`, `ValidateDescription`, `ValidateOwnerEmail`, `ValidateStatus`).

Then one folder per use case, each with input, validator and handler:

```csharp
public sealed record CreateProjectInput
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string Owner { get; init; }
}

public sealed class CreateProjectInputValidator : ProjectValidator<CreateProjectInput> { … }

public sealed class CreateProjectHandler(IProjectRepository repository, ValidatorSet<CreateProjectInput> validators)
    : ValidationService, IHandler<CreateProjectInput, ProjectDto>
{
    public Task<Result<ProjectDto>> HandleAsync(CreateProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            // uniqueness pre-check, insert, map
        }, cancellationToken);
}
```

Rules:

- `Owner` on create, `VisibleOwner` on reads/updates/deletes, `Owners` on a filter request,
  `LastWriter` on update. `CreatedBy`/`UpdatedBy` are audit column names, not input names.
- Validation is shape only. Uniqueness is a business rule: check the port and return `ConflictError`; the
  unique index still protects the race.
- Update patches only the supplied fields, sets `UpdatedAt`/`UpdatedBy` only when something changed, and
  returns the current row when nothing changed. The uniqueness check uses the owner of the **stored** row.
- Not found is `NotFoundError`; an invalid field the validator cannot see is `BadRequestError`.

## 4. Infrastructure

`src/CleanWebApiTemplate.Infrastructure/{Feature}/`

1. `{Feature}Table.cs` — `Name`, every column constant, `UniqueTitleIndexName`, `PrimaryKeyName`, and a
   `Columns` list in the exact order the mapper reads.
2. `{Feature}SortColumns.cs` — the whitelist map from `nameof({Feature}Entity.X)` to the column constant.
3. `{Feature}Repository.cs` — `sealed class(NpgsqlDataSource dataSource) : I{Feature}Repository`. Build the
   fixed SQL from `{Feature}Table`; bind every value; map every row explicitly in `Map{Feature}`; map
   `PostgresException` with `UniqueViolation AND ConstraintName == {Feature}Table.UniqueTitleIndexName` to
   `ConflictError`; convert every `timestamptz` with `ToUtc`.

The repository opens one connection per operation with `await dataSource.OpenConnectionAsync(ct)` and
disposes it with `await using`. There is no unit of work and no transaction helper: a use case that needs
several statements in one transaction adds an explicit transaction method to the port, never a shared
connection across the layers.

## 5. API

`src/CleanWebApiTemplate.Api/{Feature}/`

1. `{Feature}Response.cs`, `{Feature}TitleResponse.cs` — `sealed record` implementing
   `IBaseResponse<TDto, TResponse>` with a non-null `static ToResponseModel`.
2. `{UseCase}/{Verb}{Feature}Request.cs` — `sealed record` with `ToInput(OwnerScope owner)` (and
   `ToInput(string id, OwnerScope owner)` for update).
3. `{Feature}Endpoints.cs` — one static method per operation: mapping, handler call, `ToResponse` /
   `ToCreatedResponse` / `ToNoContentResponse`. Inject `ICurrentUser`, never a repository.
4. `{Feature}Routes.cs` — `sealed class : IGroupMap`, the groups (`User`, `Operator`), the metadata and the
   documented status codes.
5. `{Feature}JsonSerializerContext.cs` — every request, response and collection type.
6. `{Feature}OpenApiSchemas.cs` — the tag and the schema descriptions (`DescribePaging`, `DescribeSort`,
   `DescribeStatus<TEnum>`).
7. `{Feature}Module.cs` — `Add{Feature}Module(this IServiceCollection services)`:
   `services.AddScoped<I{Feature}Repository, {Feature}Repository>()`, one
   `services.AddUseCase<TInput, TResult, THandler, TValidator>()` per use case,
   `services.TryAddEnumerable(ServiceDescriptor.Singleton<IGroupMap, {Feature}Routes>())`,
   `services.ConfigureHttpJsonOptions(… {Feature}JsonSerializerContext.Default)`,
   `services.Configure<OpenApiOptions>(OpenApiConfiguration.DocumentName, … {Feature}OpenApiSchemas…)`.
8. `ConfigureServices.AddApiServices`: add `services.Add{Feature}Module();`.

## 6. Database and local environment

1. `local/database/000N__{features}_schema.sql` — table with `Id bytea`, audit `timestamptz`, `Status integer`,
   `CreatedBy`/`UpdatedBy text`, `PK_{Table}`, `UX_{Table}_CreatedBy_Title`, indexes, `IF NOT EXISTS`.
2. `local/database/seed/000N__{features}.sql` — rows for every identity and every status, matching the
   devtoken emails, `ON CONFLICT ("Id") DO NOTHING`.
3. `local/openapi/examples.json` — one entry per operation, with `parameters` as an array, bodies and
   responses consistent with the seed. Add the files under `bodies/` and `responses/`. See
   `local-swagger.md`.
4. A new role or identity also touches `local/tools/devtoken.json`, `local/openapi/users.json`,
   `Constants`/`ConfigureAuth`, `TestUser`/`TestUsers` and `tests/README.md`.

`db-init` applies every `*.sql` script in ordinal order, so a new table needs no AppHost change. A new secret
does: `openbao-init` writes it and the AppHost passes it as a parameter.

## 7. Tests

| Project | What to add |
|---|---|
| `CleanWebApiTemplate.UnitTests` | One class per validator and per handler (NSubstitute port, real `ValidatorSet`), the sort alignment, the request mapping, the JSON contract |
| `CleanWebApiTemplate.Api.ComponentTests` | Endpoints, filter, ownership (admin vs non-admin), authorization matrix, validation at the HTTP edge, repository uniqueness, OpenAPI contract (add the new operation ids and paths to `OpenApiContractTests`) |
| `CleanWebApiTemplate.Api.FunctionalTests` | One lifecycle flow and, when auth changes, the invalid-token matrix |
| `CleanWebApiTemplate.ArchitectureTests` | Nothing when the folder conventions are followed; the rules are generic |
| `CleanWebApiTemplate.Tests.Shared` | `{Feature}Builder`, `{Feature}TestRepository`, the fixture property; a new database adds a reset name |

The sort alignment test lives in the unit project next to the map it proves:

```csharp
Assert.Equal([.. ProjectSortCatalog.KeySet.Order()], [.. ProjectSortColumns.Map.Keys.Order()],
             StringComparer.OrdinalIgnoreCase);
```

## 8. Docs

- `README.md` (generated projects): the API table (method, route, policy, success) and any new setting.
- `local/local-development.md`: seed coverage when the seed changes.
- `CHANGELOG.md`: `Added`/`Changed` under `[Unreleased]` when the repository keeps one.

## Adding one endpoint to an existing feature

1. Request (if it has a body) and response (if it returns a shape).
2. Input + validator + handler under the feature and use-case folder.
3. Port method if a new query is needed (then Infrastructure + tests).
4. Endpoint method in `{Feature}Endpoints`, route + metadata in `{Feature}Routes`.
5. `AddUseCase<…>()` in the module; JSON context entry; OpenAPI descriptors and documented statuses.
6. Examples and tests (component minimum; functional when it is part of a flow).
7. README table row.

## Adding a column

1. Domain entity and constraints; enum if the column is a status.
2. Application DTO, input, validator, mapping; projection/filter if it participates.
3. Infrastructure table constant, `Columns` list, INSERT/UPDATE parameters, `Map{Function}`, sort map if
   sortable.
4. A **new** SQL script with `ALTER TABLE … ADD COLUMN IF NOT EXISTS`; update the seed.
5. API request/response, JSON context (only if a new contract type), OpenAPI descriptors.
6. Local examples and the tests listed in § 7.

Never edit a deployed script and never change the column order the mapper relies on without updating
`Map{Function}` in the same change.

## Removing the sample feature

Search first: `git grep -n -i todo`. Then, in this order:

1. **Api**: delete `Api/Todo`; remove `services.AddTodoModule();` from `ConfigureServices`.
2. **Application**: delete `Application/Todo`.
3. **Domain**: delete `Domain/Todo`.
4. **Infrastructure**: delete `Infrastructure/Todo`.
5. **Database**: delete `local/database/**/*todo*`; `db-init` is generic and needs no change.
6. **Local OpenAPI**: remove the `Todo` entries from `examples.json` and their `bodies/` and `responses/`
   files; keep the profiles (they are role-based).
7. **Tests**: delete `ComponentTests/Todo`, `FunctionalTests/Todo`, the Todo unit tests, and
   `Tests.Shared/Data/{TodoBuilder,TodoTestRepository}.cs`; remove `Todos` from `TestServerFixtureBase`
   (property, creation and disposal); delete the `TodoDb` reset name if no other database uses it.
8. **Test identities**: `TestUser.TodoUser/…` are role identities, not Todo code. Rename them to
   `User/Operator/Admin/External` and update `TestUsers` (same subject and email) if the names bother you;
   the ids must stay stable.
9. **Architecture tests**: they are generic, but `DependencyRulesTests` and `StructureTests` use
   `typeof(TodoEntity)`, `typeof(TodoRepository)`, `typeof(TodoRoutes)` and `typeof(ITodoRepository)` as
   assembly anchors. Point them at any remaining type of the same assembly (`typeof(IHandler<,>)`,
   `typeof(Result<>)`, `typeof(Program)`) before deleting the sample.
10. **Contract tests**: `OpenApiContractTests` enumerates the Todo operations and `ApiContractTests` uses
    `/api/todos`. Rewrite them for the first real feature or delete them with the sample.
11. **Docs**: update the README API table and the local seed coverage.

Then run the gates. A build error that mentions `Todo` after step 7 is an anchor you missed, not a file you
should keep.

## After removing the sample

The `Project` example in `assets/examples/` shows the full slice with the same conventions. Copy it in layer
order, then replace `Project`/`project` with the real feature name. Do not copy `Todo` from the template
repository into the generated project.
