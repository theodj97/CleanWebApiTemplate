# Conventions

One naming law, one shape per kind of type, one folder per feature. These are the rules an agent is most
likely to break, so they come first.

## Naming law

| Kind | Pattern | Example |
|---|---|---|
| Entity | `{Thing}Entity` in `Domain/{Feature}` | `TodoEntity` |
| Enum | `E{Thing}` | `ETodoStatus` |
| Business constants | `{Feature}Constraints` | `TodoConstraints.TitleMaxLength` |
| Port | `I{Thing}Repository` in the feature | `ITodoRepository` |
| Use case input | `{Verb}{Thing}Input` | `CreateTodoInput` |
| Validator | `{Verb}{Thing}InputValidator` | `CreateTodoInputValidator` |
| Handler | `{Verb}{Thing}Handler` | `CreateTodoHandler` |
| Read model | `{Thing}Dto` | `TodoDto` |
| Projection | `{ThingTitle}` / `{Thing}Summary` | `TodoTitle` |
| Query contract | `{Thing}Filter` | `TodoFilter` |
| Sort vocabulary | `{Thing}SortCatalog` (Application) and `{Thing}SortColumns` (Infrastructure) | `TodoSortCatalog` |
| Table metadata | `{Thing}Table` | `TodoTable` |
| Request | `{Verb}{Thing}Request` / `{Verb}ed{Thing}Request` | `CreateTodoRequest`, `FilteredTodoRequest` |
| Response | `{Thing}Response` | `TodoResponse` |
| Endpoint adapter | `{Thing}Endpoints` | `TodoEndpoints` |
| Route group | `{Thing}Routes` | `TodoRoutes` |
| Composition module | `{Thing}Module` | `TodoModule` |
| JSON context | `{Thing}JsonSerializerContext` | `TodoJsonSerializerContext` |
| OpenAPI descriptors | `{Thing}OpenApiSchemas` | `TodoOpenApiSchemas` |
| Test | `Method_Scenario_ExpectedResult` | `Create_WithDuplicateTitle_ReturnsConflict` |

Plurals are not used for entity or table names. The HTTP route is lowercase and plural (`/api/todos`), the
tag is the title-cased plural (`Todos`), the table is singular and PascalCase (`Todo`).

Type names are spelled out: `Todo`, never `ToDo`, `TODOS`, `Item` or `Task` in code. The domain glossary at
the repository root is the reference for business words.

## Type shape

| Kind | Shape |
|---|---|
| Request, response, input, DTO, filter | `sealed record` with `required` for mandatory members and `init` |
| Projection | positional `sealed record` |
| Handler, validator, repository, route group, module | `sealed class` |
| Entity | `sealed class`, mutable (`set`), because the repository materialises and patches it |
| Constants holder | `static class`, PascalCase members |
| Enum | `E` prefix, explicit `int` values starting at 1 |

Nullability is meaningful: a `string?` in a request means "omitted"; the mapping turns it into the domain's
default (`string.Empty`). A non-nullable property in a response is a promise the mapper must keep.

`TodoDto` is a flat record with `required` properties. `TodoResponse` repeats the shape because it is the HTTP
contract: the API owns the wire format and maps explicitly. Do not serialize a DTO or an entity directly.

## One type per file

One public type per file, named after it. Exceptions that already exist and are intentional:

- `Error.cs` groups the small error records: they exist only as a family.
- `BaseAbstractValidator.cs` also declares `DateRange`, the strongly-typed range used by cross-field rules.
- `AuthenticationSettings.cs` declares the internal mode enum next to the record.
- JSON-context files declare only the context; their DTOs live in the OpenBao feature folder.

## Folders

`{Layer}/{Feature}` for feature code, `{Layer}/Common` for shared code. Use case folders are named after the
verb, not the HTTP method: `Create`, `GetById`, `Filter`, `Update`, `Delete`, `GetTitles`. A feature with one
route still gets its own folder in every layer.

Tests mirror the production path under the test project: `tests/CleanWebApiTemplate.UnitTests/
Application/Todo/Create/CreateTodoHandlerTests.cs`.

## Routes

- Lowercase, plural, no verb in the path: `GET /api/todos/{id}`, `POST /api/todos/filter`.
- `POST` for a structured search (`filter`, `titles`); `GET` only for a resource by id.
- The route group owns the prefix, tag and policies; the endpoint adapter owns the plumbing.
- Operation ids are stable and unique across the document (`GetTodoById`, `FilterTodos`, `CreateTodo`).
- Document every status the operation can return with `Produces`/`ProducesProblem`.

## SQL

- Schema and seed live in `local/database/*.sql` and `local/database/seed/*.sql`, applied in ordinal order.
- Names are quoted PascalCase (`"Todo"`, `"CreatedBy"`), matching `TodoTable`.
- Scripts are forward-only: never edit a script that has been deployed; add a higher-numbered script.
- Indexes: `PK_{Table}`, `UX_{Table}_{Columns}` for unique, `IX_{Table}_{Column}` for the rest.
- Values are always bound parameters; identifiers always come from constants.

## C#

- File-scoped namespaces; `using` directives outside the namespace, `System` first.
- Collection expressions (`[]`, `[.. items]`) and target-typed `new()`.
- `is null` / `is not null` / `is false` rather than `== null`.
- Primary constructors for handlers, repositories and adapters.
- `sealed` on every class that is not designed for inheritance. Validators are sealed; the shared base
  (`TodoValidator<T>`, `BaseAbstractValidator<T>`) is abstract.
- `[LoggerMessage]` partial methods for logging; no string interpolation in log calls.
- `var` where the type is obvious from the right-hand side; explicit types where it is not.
- Private fields use `_camelCase`. Primary-constructor parameters captured in a field keep the same name
  (assigned to a `private readonly` field when the class needs it).

`.editorconfig` is authoritative: IDE0005 (unnecessary using) is a warning, IDE0060 (unused parameter) is an
error, IDE0161 (file-scoped namespace) is a warning, and `EnforceCodeStyleInBuild` makes them part of the
build. If the signature needs the parameter, rename it to `_` or `_1`.

## Tests

- `Method_Scenario_ExpectedResult`, Arrange/Act/Assert with a single action.
- `[ResetDatabase]` and `[AuthenticatedAs(...)]` at method level unless every test in the class shares them.
- Builders (`TodoBuilder`) for data, `TestData.Unique` for unique values, `Eventually` for eventual effects.
- Never `Thread.Sleep`, a fixed `Task.Delay`, a hardcoded id or a token from a login endpoint.

## Documentation

- `README.md` (generated projects) documents the API table, configuration keys and gates.
- `AGENTS.md` holds the rules for coding agents; this skill holds the detail.
- `GLOSSARY.md` holds domain language only.
- `docs/adr/` records decisions that are hard to reverse and surprising without context.

## Commits

`<type>(<optional scope>): <description>` with 1–72 characters. Types: `feat`, `fix`, `refactor`, `docs`,
`test`, `chore`, `build`, `perf`, `revert`. `Merge`, `Revert`, `fixup!` and `squash!` are accepted by the hook.
The body explains why, not what. Hooks: gitleaks + format on `pre-commit`, the subject rule on `commit-msg`,
the full gates on `pre-push`.
