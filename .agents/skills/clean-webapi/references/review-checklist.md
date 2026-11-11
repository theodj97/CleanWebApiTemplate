# Review checklist

Use this for a repository-specific review. For the full two-axis review (Standards + Spec), run the
`code-review` skill and give it this file and `conventions.md` as the standards sources.

## How to review

1. Pin the fixed point and capture `git diff <fixed-point>...HEAD` (or the working tree if the user asked for
   that). Confirm it is non-empty.
2. Read `AGENTS.md`, the README architecture/API sections and the nearest existing feature.
3. Map every changed file to a layer. A file in the wrong layer is a structural finding, even if the code
   works.
4. Apply the hard rules first; they are violations, not style preferences.
5. Then apply the judgement checks. Label each finding with severity and cite `file:line`.
6. Report findings; do not rewrite the code unless the user asked for fixes.

## Hard rules

| Rule | Failure signal | Fix |
|---|---|---|
| Dependency direction | `Application` references `Infrastructure`, `Microsoft.AspNetCore.*` or `Npgsql`; `Domain` references any outer project | Move the port inward, the adapter outward, or the type to the correct layer |
| Native AOT | `System.Reflection`, `dynamic`, reflection JSON, Swashbuckle, `RequiresDynamicCode`, `RequiresUnreferencedCode`, `UnconditionalSuppressMessage` | Replace with source-generated/static code; run the AOT publish |
| Explicit registration | A new validator/handler/repository/route group/JSON contract is not registered | Add it to `ConfigureServices`, `EndpointExtension`, `AppJsonSerializerContext`, OpenAPI descriptors |
| Result/Error | A handler throws for validation/not-found/conflict; HTTP status in Application; an `Error` type not mapped; a second ProblemDetails body builder | Return `Result<T>`/`Error`; map the status in `ApiResultExtensions`; build every body through `ProblemDetailsFactory`; document status |
| SQL safety | String interpolation of user input; sort key used directly; a column name from a request | Bound parameters; `*Table` constants; `*SortCatalog` accepted keys + `*SortColumns` map |
| Ownership | `createdBy`/owner read from the body or query for authorization; non-admin can omit the owner filter; `OwnerScope` treated as a nullable string | Use `ICurrentUser.Owner`; `VisibleOwner`/`OwnerForCreate`/`FilterOwners`; repository filters by owner |
| Secrets | A secret/connection string in `appsettings*.json`, a test, a log or a report; `API_KEY` passed to the app as env | OpenBao + `openbao-init`; redact; rotate |
| Tests | No test for a new behaviour; a test sleeps; hardcoded id/token; reset after the test; a test obtains a token from login | Add the right test at the right level; `Eventually`; `TestData`; `[ResetDatabase]`; `TestTokenIssuer` |
| Gates | A warning is suppressed; coverage threshold lowered; a test project skipped without reporting | Fix the cause; add tests; report what could not run |
| Contract | Endpoint/response/schema changed without OpenAPI/JSON/README updates | Add the contract entry and the doc row |

## Layer checks

### Domain

- [ ] Entity has `Id` (ULID), audit fields and a status default that matches the enum.
- [ ] Enum values are explicit and start at 1.
- [ ] Constraints live in `{Feature}Constraints`, not in SQL/validators.
- [ ] No DTO, HTTP, configuration, `Result<T>`, or framework attribute.

### Application

- [ ] Input is a `record` with `required`/`init`; audit/owner fields are present where needed.
- [ ] Validator validates shape only; uniqueness/business rules are in the handler.
- [ ] Handler implements `IHandler<TInput, TResult>`, derives from `ValidationService`, takes the port +
      `ValidatorSet<TInput>`, and exposes `HandleAsync` returning `Result<TResult>`.
- [ ] Every handler has a registered `ValidatorSet<T>`; `ValidateOnBuild` is enabled, so a missing set fails
      at startup.
- [ ] Business failures are typed `Error`s; no `throw` for expected outcomes.
- [ ] Port uses Domain/Application types only; the filter is persistence-neutral.
- [ ] Partial reads return a projection from `Application/{Feature}/Projections`, not a partially filled
      entity/DTO.
- [ ] DTO/projection mapping is in Application; the API response mapping is not.
- [ ] No `HttpContext`, `IResult`, `Npgsql*` or connection string.

### Infrastructure

- [ ] Table/column names come from `{Feature}Table`; accepted sort keys from `{Feature}SortCatalog`; physical
      mapping from `{Feature}SortColumns`.
- [ ] `{Feature}SortCatalog.KeySet` and `{Feature}SortColumns.Map.Keys` agree; a test proves it.
- [ ] Every value is a bound parameter with an explicit `NpgsqlDbType`.
- [ ] Row mapping is manual, in the `{Feature}Columns` order, with `IsDBNull` checks.
- [ ] ULID ↔ `bytea`, UTC ↔ `timestamptz`.
- [ ] Unique violation → `ConflictError`; no broad `PostgresException` catch.
- [ ] No business rule beyond translating a constraint violation.

### API

- [ ] Route group uses the narrowest policy; the request → input → handler → response plumbing lives in
      `{Feature}Endpoints`, not in the route lambdas.
- [ ] Every operation has name/summary/description/produces.
- [ ] Request → input takes `currentUser.Owner`; create uses `OwnerForCreate`, reads use `VisibleOwner`,
      filter uses `FilterOwners`.
- [ ] Result mapped through `ToResponse(context)`/`ToCreatedResponse(context, ...)`; no manual status code
      switch.
- [ ] New contracts in `AppJsonSerializerContext`.
- [ ] New schemas in `{Feature}OpenApiSchemas`, with `nameof` and `{Feature}Constraints`; the tag is registered;
      descriptions name this feature's fields and statuses, not the Todo sample's.
- [ ] New `Error` mapped in `ApiResultExtensions.ResolveError` (body through `ProblemDetailsFactory`) and
      documented.
- [ ] ProblemDetails consistency (400/404/405/malformed JSON), including the `traceId` and uniform `type`.

### Database/local

- [ ] Schema script ordered and transactional; seed separate and local-only.
- [ ] Unique index matches the business rule; indexes cover the filters.
- [ ] Seed uses devtoken emails/roles and covers every state.
- [ ] `db-init` readiness query references an existing table.
- [ ] No volume/persistent container; no external service.

### Tests

- [ ] Validator rules covered, including null/empty/length.
- [ ] `ValidatorSet` empty-set failure and aggregation covered.
- [ ] `OwnerScope` admin/non-admin methods covered.
- [ ] Sort catalog/column map alignment covered.
- [ ] Handler success + each business failure + port failure propagation.
- [ ] Component success/400/404/204/409/persistence/ProblemDetails.
- [ ] Authorization matrix for every new protected route (401/403/2xx per role).
- [ ] Ownership: non-admin vs admin, another owner.
- [ ] Functional flow when the feature spans endpoints.
- [ ] Architecture test for a new port/filter/repository location.
- [ ] If a second feature adds repository helpers, the hardcoded `Todo.Helpers` condition in `StructureTests`
      was generalized to `.Contains(".Helpers")` (or the new prefix was added).
- [ ] OpenAPI contract test for operation ids/status codes.
- [ ] `[ResetDatabase]`, `[AuthenticatedAs]`, `TestData`, builder, `Eventually`; no sleeps/hardcoded ids.
- [ ] One new test was shown to fail without the change.

### Docs

- [ ] README API table row for every new endpoint.
- [ ] `tests/README.md` updated only if categories/identities/coverage changed.
- [ ] `local/local-development.md` updated if topology/seed/tokens changed.
- [ ] `AGENTS.md` updated only if AOT/hook rules changed.

## Judgement checks

These are not hard violations unless they cause a real problem; label them as suggestions.

- **Duplicate query shape**: two repositories building the same filter; extract a helper or share the SQL
  builder.
- **Fat handler**: a handler doing several use cases; split by verb.
- **Anemic validator**: a validator that repeats a constant instead of using `{Feature}Constraints`.
- **Leaky DTO**: a DTO with a field the feature never exposes; remove it.
- **Over-broad policy**: an endpoint on `Admin`/`Operator` when `User` suffices.
- **Missing projection**: a list endpoint selecting a subset into an all-nullable DTO and casting later; use a
  projection type.
- **Magic status**: an integer literal instead of the enum.
- **Stringly sort**: a sort key list duplicated in validators, OpenAPI and SQL; keep the catalog as the one
  vocabulary and the column map as the one translation, and derive docs from the catalog.
- **Test redundancy**: a component test that repeats a unit test without adding the HTTP/database dimension.
- **Slow test**: a component test that could be a unit test; move it.
- **Docs drift**: an API table that no longer matches the routes.

## Output format

```markdown
## Findings

### <severity>: <title> (`path:line`)
- Rule: <hard rule / judgement check>
- Evidence: <quote the hunk>
- Fix: <one sentence>

...

## Summary
<count per severity; the worst structural issue; what was not reviewed>
```

Severity:

- **Blocker** — hard rule violated (architecture, AOT, security, SQL, missing tests for a new behaviour).
- **Major** — contract/doc drift, missing registration, wrong layer, weak test coverage.
- **Minor** — naming/style/judgement suggestions.

Do not merge findings across axes in a `code-review` run; report Standards here and let the Spec axis report
separately.
