# Review checklist

A diff review rubric for this architecture. Findings come in severity order; every finding cites
`file:line`, the rule and a one-line fix. Do not rewrite the code while reviewing.

## Blockers

| Check | What "wrong" looks like |
|---|---|
| Dependency rule | `Application` references `Infrastructure`/ASP.NET Core; `Domain` references a framework or `Ulid` is replaced by a SQL type; `Infrastructure` references `Api`. |
| SQL in the API | A `NpgsqlCommand`, a connection string or a query in `Api/`; the endpoint injects a repository instead of a handler. |
| Owner from input | The request body sets the owner; the mapper does not use `OwnerScope`; a non-admin can pass `Owners` and see other rows. |
| Missing JSON metadata | A request/response type returned or accepted by an endpoint is not in the feature `JsonSerializerContext` (`JsonContractTests` should fail). |
| Reflection / dynamic | `System.Reflection`, `dynamic`, `Activator`, `Enum.GetValues(Type)`, reflection JSON, a suppression attribute (`RequiresDynamicCode`, `UnreferencedCode`, `UnconditionalSuppressMessage`). |
| Secret in configuration | A key, connection string or token in `appsettings*.json`, a test fixture, a committed `.env`, a log line. |
| Unregistered use case | A handler exists without `AddUseCase<…>()`; a validator exists without being reachable from the set; a route group is not registered in the module. |
| Test sleeps | `Thread.Sleep` or a fixed `Task.Delay` in a test instead of `Eventually`. |
| Swagger example missing | A new operation has no `examples.json` entry (or its body/response files). |

## Major

| Check | Expected |
|---|---|
| Unique index mapping | The `PostgresException` filter checks `SqlState == UniqueViolation` **and** `ConstraintName`; a broad catch reports a PK collision as a duplicate title. |
| Parameterisation | Every value is a bound parameter; identifiers come from `{Feature}Table`; an IN list has one parameter per element. |
| Sort alignment | `{Feature}SortCatalog.KeySet` and `{Feature}SortColumns.Map` name the same keys and a unit test proves it. |
| Type shape | Contracts are `sealed record` with `required`/`init`; handlers, validators, repositories, routes are `sealed class`; entities are mutable. |
| Status codes | GET missing/invisible 404, empty list `200 []`, DELETE 204/404, PUT 200/404, POST 201/409, unknown errors 500. |
| ProblemDetails | Errors go through `Result` + `ApiResultExtensions`; no manual `Results.Problem` in a handler; `traceId` present. |
| Validation split | Shape rules in the validator; uniqueness in the handler (pre-check) and the index (race). |
| Update semantics | Only supplied fields change; `UpdatedAt`/`UpdatedBy` only when something changed; unchanged row returns current state. |
| Transaction scope | A connection or transaction never crosses the repository boundary; no shared connection in a handler. |
| OpenAPI | Every operation has an id, summary, description and documented statuses; the feature tag exists; the schema describes lengths, enums and formats from the constraints. |
| Tests | New behaviour has a failing-without-the-change test; authorization matrix covers 401/403; ownership covered for admin and non-admin. |
| Naming | `{Feature}` folders and type names; no synonyms (`Item`, `Task`, `Record`); routes lowercase plural. |

## Minor

| Check | Expected |
|---|---|
| Usings | Sorted, `System` first, none unnecessary (IDE0005). |
| Nullability | No `!` at the response edge; `required` where the mapper guarantees a value. |
| Duplication | The shared rule lives in the feature validator or a `Common` helper, not copy-pasted. |
| Comments | Explain why, not what; no stale references to removed types or files. |
| Docs | README API table, local seed coverage and examples updated when the contract or the seed changed. |
| Commit message | Conventional subject, body explains why. |

## Failure signals to search for

```bash
git grep -n -E "System\.Reflection|dynamic |Activator\.|Enum\.GetValues\(|UnconditionalSuppressMessage" -- src/
git grep -n "CreatedBy = " -- src/CleanWebApiTemplate.Api   # owner should come from OwnerScope
git grep -n "Npgsql|Dapper|DbContext" -- src/CleanWebApiTemplate.Api
git grep -n "Thread\.Sleep|Task\.Delay" -- tests/
git grep -n "API_KEY|Password=|Token=" -- "*.json" "*.md"
```

## The report

Split findings into **blockers**, **major** and **minor**. Each one: file:line, the rule, the fix. Finish
with what you did not review (for example, the local environment was not started) and why.
