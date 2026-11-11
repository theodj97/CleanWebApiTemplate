# Conventions

The repository has a small naming law. Agents that follow it produce code that looks native to the template;
agents that invent names create a second convention that the architecture tests cannot protect.

## Folder map

```text
src/{App}.Api/
├── Common/                 ICurrentUser adapter, route builder extensions
├── Configuration/          AppSettings, sections, validators, OpenAPI, JSON context, ProblemDetails
├── Extensions/             ApiResultExtensions, EndpointExtension
├── Helpers/                configuration loader
├── Models/                 response contracts (`Models/Responses/{Feature}`) and interfaces
├── Routes/{Feature}/       route group, `{Feature}Endpoints` adapter, `{Verb}` request contracts
├── ConfigureApplication.cs
├── ConfigureServices.cs
└── Program.cs

src/{App}.Application/
├── Abstractions/
│   ├── Handlers/          `IHandler<TInput, TResult>`
│   ├── Identity/          `ICurrentUser`, `OwnerScope`
│   ├── Repositories/      `I{Feature}Repository`
│   └── Services/          shared service interfaces (only when two handlers share behaviour)
├── Dtos/{Feature}/         `{Feature}Dto`, `{Feature}DtoExtensions`
├── Results/                `Result<T>`, `Error` hierarchy
├── Validation/             `ValidationService`, `ValidatorSet<TInput>`, `BaseAbstractValidator`
└── {Feature}/
    ├── Handlers/
    │   ├── {Feature}Validator.cs      shared rules for the feature
    │   ├── Queries/                   `{Feature}Filter`
    │   ├── Create/                    Input, Validator, Handler
    │   ├── Delete/
    │   ├── Filtered/
    │   ├── GetById/
    │   ├── GetTitles/                 handler and input; the projection lives in `Projections`
    │   └── Update/
    ├── Projections/                   `{Projection}.cs` when a read selects a subset (`TodoTitle`)
    └── Sorting/                       `{Feature}SortCatalog.cs` (accepted keys)

src/{App}.Domain/
├── Models/
│   ├── BaseEntity.cs
│   ├── Entities/{Feature}Entity.cs
│   ├── Enums/{Feature}/E{Feature}Status.cs
│   └── {Feature}/{Feature}Constraints.cs
└── ...

src/{App}.Infrastructure/
├── Data/                   `INpgsqlConnectionFactory`, `NpgsqlConnectionFactory`
├── Repositories/{Feature}/
│   ├── {Feature}Repository.cs
│   └── Helpers/{Feature}Table.cs, {Feature}SortColumns.cs
└── Secrets/                OpenBao adapter

tests/{App}.Api.UnitTests/          mostly mirrors `src/`; validator/endpoint tests may sit under the feature folder
tests/{App}.Api.ComponentTests/     `{Feature}/` endpoint, auth, ownership, validation tests
tests/{App}.Api.FunctionalTests/    `{Feature}/` complete flows
tests/{App}.ArchitectureTests/      dependency and structure rules
tests/{App}.Tests.Shared/           fixtures, attributes, auth, data, database, http, waiting
```

`Services` holds the interfaces of services shared by **several handlers** (for example, a title uniqueness
service used by create and update). It is not a home for use cases: one handler per use case stays in
`{Feature}/Handlers/{Verb}`. Do not create a service for a single handler; keep the behaviour in that handler
until a second one needs it.

Keep a test next to the closest existing test: the template puts handler tests under
`Api.UnitTests/Application/{Feature}/` and input-validator tests under `Api.UnitTests/{Feature}/`. The folder
should make the production type obvious; do not create a flat `Services/` or `Controllers/` test folder.

## Naming law

| Concept | Pattern | Example |
|---|---|---|
| Domain entity | `{Feature}Entity` | `ProjectEntity` |
| Domain enum | `E{Feature}{Concept}` | `EProjectStatus`, `ETodoStatus` |
| Domain constraints | `{Feature}Constraints` | `ProjectConstraints` |
| Application input | `{Verb}{Feature}Input` | `CreateProjectInput`, `UpdateProjectInput` |
| Input validator | `{Verb}{Feature}InputValidator` | `CreateProjectInputValidator` |
| Shared feature rules | `{Feature}Validator<TInput>` | `ProjectValidator<TInput>` |
| Handler | `{Verb}{Feature}Handler` | `CreateProjectHandler` |
| Handler contract | `IHandler<{Verb}{Feature}Input, TResult>` | `IHandler<CreateProjectInput, ProjectDto?>` |
| Validation set | `ValidatorSet<TInput>` | `ValidatorSet<CreateProjectInput>` |
| Shared service interface | `I{Concept}Service` | `ITodoTitleService` (hypothetical; the template has none yet) |
| Query filter | `{Feature}Filter` or `{Verb}{Feature}Filter` | `ProjectFilter` |
| DTO | `{Feature}Dto` | `ProjectDto` |
| DTO mapping | `{Feature}DtoExtensions` | `ProjectDtoExtensions` |
| Projection | `{Projection}` | `TodoTitle`, `ProjectTitle` |
| Repository port | `I{Feature}Repository` | `IProjectRepository` |
| Repository adapter | `{Feature}Repository` | `ProjectRepository` |
| Table metadata | `{Feature}Table` | `ProjectTable` |
| Sort catalog | `{Feature}SortCatalog` | `ProjectSortCatalog` |
| Sort column map | `{Feature}SortColumns` | `ProjectSortColumns` |
| API request | `{Verb}{Feature}Request` | `CreateProjectRequest` |
| API response | `{Feature}Response` or `{Projection}Response` | `ProjectResponse` |
| Route group | `{Feature}Routes` | `ProjectRoutes` |
| Endpoint adapter | `{Feature}Endpoints` | `ProjectEndpoints` |
| Error | `{Status}Error` | `BadRequestError`, `ConflictError` |
| Test class | `{TypeUnderTest}Tests` | `CreateProjectHandlerTests` |
| Test method | `Method_Scenario_ExpectedResult` | `HandleAsync_WithDuplicateName_ReturnsConflict` |
| Test builder | `{Feature}Builder` | `ProjectBuilder` |
| Test repository | `{Feature}TestRepository` | `ProjectTestRepository` |

Names are **singular** for a feature (`Project`, not `Projects`), except the folder that groups several use
cases (`Application/Project/Handlers/`). Verbs are `Create`, `Update`, `Delete`, `GetById`, `Filtered`, `GetTitles`.
The route group name is the feature name and is used as the OpenAPI tag.

## File conventions

- One public type per file, file name equals the type name (`ProjectDto.cs`).
- File-scoped namespaces; namespace mirrors the folder path (`{App}.Application.Project.Handlers.Create`).
- `sealed` by default for classes that are not designed for inheritance; entities may be `sealed`.
- Primary constructors for handlers, repositories and adapters.
- `required` for non-nullable contract members that must be supplied; `init` for immutable input properties.
- `record` for inputs, DTOs and query filters; `class`/`record` for request/response contracts as the existing
  feature does; `sealed class` for entities, handlers and services.
- `internal` for helpers and mappings that do not cross the assembly boundary; `public` only for the contract
  a host needs.
- XML comments on public API contracts (the API project generates the documentation file for OpenAPI).
- `using` directives sorted; the build treats warnings as errors and `dotnet format` enforces style.

C# style seen across the repository (follow it even where the analyzer does not force it):

- `is false` / `is not null` instead of `== false` / `!= null`.
- Collection expressions (`[]`, `[.. items]`) instead of `new List<T>()` where the target type is clear.
- `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`.
- Async methods end in `Async` and take `CancellationToken cancellationToken` last.
- `Task<Result<T>>` for application operations; `Task<T?>`/`Task<bool>` for repository reads/deletes without a
  business failure. Handlers expose `HandleAsync` and implement `IHandler<TInput, TResult>`.
- Constants in `UPPER_SNAKE_CASE` (`Constants.API_KEY`, `Constants.USER_POLICY`); private constants may be
  PascalCase (`TodoColumns`, `DuplicateTitleMessage`).
- Private fields `camelCase`, with `_` only in test fixtures/helpers (`_repository`, `_clients`); production
  code uses primary-constructor parameters and explicit fields without underscore where the style already does.
- Avoid `var` when the type is not obvious; the repository uses explicit types for results, DTOs and domain
  types.

## SQL naming

- Tables and columns are PascalCase and quoted: `"Project"`, `"CreatedAt"`.
- Primary key `PK_{Table}`; unique index `UX_{Table}_{Columns}`; regular index `IX_{Table}_{Column}`.
- Scripts are ordered `000N__description.sql`; seeds live in `local/database/seed/` with the same numbering.
- A table has `Id bytea` (16-byte ULID), `CreatedAt`/`UpdatedAt timestamptz`, `CreatedBy`/`UpdatedBy text`,
  and the status as `integer`.
- Name unique indexes after the business rule (`UX_Project_CreatedBy_Name`), not after the column order only.

## API naming

- Route group: `/api/{Feature}`; operations are relative (`/{id}`, `/filter`, `/titles`).
- Operation ids: `{Verb}{Feature}` (`CreateProject`, `GetProjectById`, `FilterProjects`, `DeleteProject`).
- OpenAPI tags: the feature name (`Project`, `Todo`).
- ProblemDetails: title = the error title or reason phrase; detail = a human-readable sentence; type =
  `https://httpstatuses.io/{status}` (the factory owns it, not the error class name).
- JSON: `JsonSerializerDefaults.Web` (camelCase), source-generated context.

## Test naming and structure

- `// Arrange`, `// Act`, `// Assert` in that order; one action in Act.
- Name: `Method_Scenario_ExpectedResult` (`CreateAsync_WithDuplicateName_ReturnsConflict`).
- Test classes mirror the production type; keep the folder next to the closest existing test (handler tests under
  `Application/{Feature}`, input-validator tests under `{Feature}`).
- Component/functional test classes are named after the behaviour (`ProjectEndpointsTests`,
  `ProjectAuthorizationMatrixTests`, `ProjectLifecycleFlowTests`), not after the controller.
- Use `TestData.Unique("project")` for unique columns, `ProjectBuilder` for valid defaults, and
  `Fixture.Projects` for arrange/assert.
- Never hardcode ids, dates or tokens; never `Thread.Sleep`/`Task.Delay`; use `Eventually`.
- `[ResetDatabase]` before a test that writes; `[AuthenticatedAs(TestUser.X)]` on the class or method;
  `[Unauthenticated]` to override a class identity; `CreateClient(TestUser.X)` when the identity varies.

## Commit convention

The `commit-msg` hook enforces:

```text
<type>(<optional-scope>): <description of 1-72 characters>
```

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`, `perf`, `revert`. Scope is lowercase and
hyphenated (`todo`, `auth`, `openapi`). The body goes after a blank line. Examples:

```text
feat(project): add create, get and filter endpoints
fix(auth): reject tokens without an email claim
test(project): cover the authorization matrix
docs: describe the local OpenBao flow
```

Do not add `Co-Authored-By` or generated-by trailers unless the user asks for them. Do not commit, push or
create branches from an agent session unless the user explicitly asks.

## Documentation conventions

- The generated README has an API table; every new endpoint updates it (method, route, policy, success,
  errors).
- `tests/README.md` describes the test projects, identities and conventions; update it only when those change.
- `local/local-development.md` describes the topology, tokens and troubleshooting; update it when the AppHost,
  seed, OpenBao path or devtoken profiles change.
- `AGENTS.md` holds the AOT and hook rules; do not duplicate them in feature docs.
