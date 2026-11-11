---
name: clean-webapi
description: >-
  Works on ASP.NET Core 10 repositories generated from CleanWebApiTemplate: Clean Architecture layers
  (Api, Application, Domain, Infrastructure, ServiceDefaults), Minimal API route groups, FluentValidation inputs
  and handlers, Result/Error, ADO.NET/Npgsql with NpgsqlDataSource, PostgreSQL schema scripts, OpenBao secrets,
  JWT modes, role policies and owner scoping, Native AOT, Aspire local development, and the unit/component/
  functional/architecture test suite. Use when adding or changing a feature, endpoint, validation rule, SQL
  table or seed row, configuration key, secret, authorization policy or test; when choosing where code belongs
  or naming a type; when fixing an IL2xxx/IL3xxx AOT warning; when reviewing a diff against the repository
  conventions; when explaining the architecture or the test suite; or when removing the sample Todo feature.
  Also for "add a feature", "add an endpoint", "add a test", "change the schema", "add a role", "why does this
  build fail with IL2026", and "review this change".
---

# Clean Web API: architecture, conventions and tests

This skill is for repositories generated from **CleanWebApiTemplate**: one solution named
`CleanWebApiTemplate` with five runtime projects, a `local/` Aspire environment and five test projects.
`dotnet new` replaces `CleanWebApiTemplate` with the generated name, so every path below keeps working.

The template ships one sample feature, **Todo**. It is the living example: read it before writing a new
feature. `assets/examples/` contains a second complete feature (**Project**) for when the sample has been
removed or changed.

## Non-negotiables

1. **Dependencies point inward.** `Api → Application → Domain`, `Api → Infrastructure → Application`,
   `Infrastructure → Application/Domain`. `Application` never references `Infrastructure` or ASP.NET Core;
   `Domain` references nothing but `System.*` and `Ulid`. `ServiceDefaults` is shared runtime code.
   → `references/architecture.md`
2. **The API stays Native AOT.** No reflection on runtime paths, no `dynamic`, no reflection-based JSON, no
   Swashbuckle schema generation, no AOT/trimming suppressions. Fix `IL2xxx`/`IL3xxx` at the cause.
   → `references/native-aot.md`
3. **Everything is registered explicitly.** No assembly scanning and no mediator. A use case is one
   `AddUseCase<…>()` call; a feature is one module (`AddTodoModule()`) that registers its repository, use
   cases, routes, JSON context and OpenAPI transformer. Contracts go in the feature's `JsonSerializerContext`.
   → `references/api.md`
4. **Business outcomes are results.** Handlers and repositories return `Result<T>` with a typed `Error`; they
   do not throw for validation, not-found or conflict. `ApiResultExtensions` chooses the status and
   `ProblemDetailsFactory` builds the body, so there is one ProblemDetails interface.
   → `references/architecture.md` § Result and Error
5. **SQL is parameterised and identifier-allow-listed.** Table and column names come from the feature's
   `*Table` constants; values are bound parameters; sort keys resolve through the feature's `*SortCatalog`
   (accepted vocabulary) and `*SortColumns` (physical map); user input never reaches SQL text.
   → `references/persistence.md`
6. **Ownership is not input.** The owner comes from the JWT email through `ICurrentUser.Owner`, an
   `OwnerScope` value type; never from a request body. `OwnerScope` decides `VisibleOwner`, `OwnerForCreate`
   and `FilterOwners`. Non-admins only see and change their own rows; admins see every owner.
   → `references/security.md`
7. **Every secret comes from OpenBao in every mode.** The application receives only `OpenBao:*` as
   environment variables; `openbao-init` writes the JWT key and the PostgreSQL connection string. Never put a
   secret or a connection string in `appsettings*.json`, a test fixture or a report.
   → `references/configuration-and-secrets.md`
8. **Every change ships with the right tests.** Unit for pure logic and validators, component for the HTTP
   pipeline and database state, functional for a complete flow, architecture for dependency and structure
   rules. → `references/testing.md`
9. **A change is done only when the gates are green.** Build with zero warnings, Native AOT publish with zero
   warnings, format, analyzers, every test and the coverage thresholds. A green build is not a green AOT
   publish. → `references/quality-gates.md`
10. **Every endpoint ships a local Swagger example.** A new operation without an entry in
    `local/openapi/examples.json` (and its bodies/responses) is not finished.
    → `references/local-swagger.md`

## Mode

Read the user's request and use the first row that matches. When two rows apply, the broader one wins.

| Request | Mode | Read |
|---|---|---|
| Add or change a feature; "add a CRUD for X"; a change that crosses layers | **Feature** (default) | `references/feature-slice.md`, then `references/architecture.md` |
| Add or change one endpoint, status code, request or response | **Endpoint** | `references/api.md`, `references/feature-slice.md` |
| Add, fix or refactor tests; "what does this test do"; coverage | **Test** | `references/testing.md` |
| Add a table, column, index, seed row, query or sort key | **Schema** | `references/persistence.md`, `references/feature-slice.md` § 4 |
| Add or change a setting, secret, connection string or OpenBao path | **Config** | `references/configuration-and-secrets.md` |
| Add a role, policy, claim or ownership rule | **Auth** | `references/security.md` |
| Start, stop, debug or extend the local environment | **Aspire** | `references/aspire.md`, `references/local-swagger.md` |
| An `IL2xxx`/`IL3xxx` warning or an AOT publish failure | **AOT** | `references/native-aot.md` |
| Review a diff or the working tree | **Review** | `references/review-checklist.md` |
| Explain the architecture, a pattern, a name or the test suite | **Explain** | `references/architecture.md`, `references/conventions.md`, `references/glossary.md`, `references/testing.md`, `references/test-catalog.md` |
| Remove the sample `Todo` feature | **Remove sample** | `references/feature-slice.md` § Removing the sample |
| A failing endpoint, test or local run | **Debug** | `references/quality-gates.md` § Troubleshooting, `references/aspire.md`, `references/native-aot.md` |

## Main flow

The flow applies to **Feature**. **Endpoint**, **Schema**, **Config**, **Auth**, **Aspire** and **AOT** run
the phases their reference names and still end with Phase 8 (gates). **Test**, **Review**, **Explain** and
**Remove sample** follow their reference's completion criteria and run the gates only when they change files.
Read `AGENTS.md` and `README.md` first; they win over this skill when they differ.

### Phase 0 — Read the repository

Identify the solution file, the five runtime projects, the test projects, the AppHost, the existing feature
closest to the change and its tests. Read `Directory.Build.props`, `Directory.Packages.props`, `global.json`,
`BannedSymbols.txt`, the API's `appsettings*.json` and `GLOSSARY.md`.

**Done when** you can name the layer and folder of every type you are about to add, and the exact files you
will touch, without editing anything.

### Phase 1 — Plan

Produce a short plan: endpoints and status codes, types per layer, schema and seed changes, registrations,
tests per project, OpenAPI examples and docs. Present it and **wait for confirmation** when the change adds a
table, a secret, a policy, a project, or touches more than one layer in a way that changes the contract.
Otherwise continue.

**Done when** every file in the plan has a named owner layer and the plan names the test that proves each
behaviour.

### Phase 2 — Domain

Add the entity, enum, constraints or value object under `Domain/{Feature}`. Domain has no framework
references and no HTTP or configuration concepts.

**Done when** the type compiles using only `System.*`, `Ulid` and Domain types, and the business constraints
live in `Domain/{Feature}/{Feature}Constraints.cs`, not in SQL or validators.

### Phase 3 — Application

Add the port, input, validator, filter, DTO, DTO mapping and handler under `Application/{Feature}`. The
handler implements `IHandler<TInput, TResult>`, derives from `ValidationService`, exposes `HandleAsync` and
returns `Result<TResult>`. It takes a `ValidatorSet<TInput>`, not a raw `IEnumerable<IValidator<TInput>>`.
Validation is input shape; uniqueness and other business rules belong to the handler. If more than one
handler needs the same behaviour that is not a repository port, add a shared abstraction under
`Application/Common`; keep one handler per use case.

**Done when** the handler has no ASP.NET, Npgsql or `IHttpContextAccessor` reference, every business failure
is a typed `Error`, the port is the only thing Infrastructure will implement, and every input has a
validator.

### Phase 4 — Infrastructure and database

Implement the port with ADO.NET/Npgsql over `NpgsqlDataSource`, plus a `{Feature}Table` helper, a
`{Feature}SortCatalog` in Application and a `{Feature}SortColumns` map in Infrastructure. Add the ordered SQL
script and the seed. Handle the unique index as a `ConflictError`.

**Done when** every value is a bound parameter, every identifier comes from a constant, the sort catalog and
the SQL map name the same keys (a unit test proves it), and the schema script applies from an empty database.

### Phase 5 — API

Add the request/response contracts, `ToInput` mapping, the `{Feature}Endpoints` adapter, the route group and
endpoint metadata. Add `{Feature}Module` with `AddUseCase<…>()`, the routes, the feature's
`JsonSerializerContext` and its OpenAPI transformer. Call the module from `ConfigureServices`.

**Done when** every endpoint returns the documented status codes, every contract type is in the feature's
JSON context, and the OpenAPI document names every operation and response.

### Phase 6 — Local environment

Add the seed rows, the OpenAPI examples, any devtoken profile or role, and any AppHost resource the feature
needs. A new table normally needs no AppHost change: `db-init` applies every `*.sql` script in order. A new
secret also needs `OpenBaoInitializer` and the AppHost parameter.

**Done when** the seed covers every state the endpoints accept, the profile picker can call every example,
and `aspire start` rebuilds the schema and seed from scratch.

### Phase 7 — Tests

Add unit, component, functional and architecture tests as the change requires. Follow the repository's
attributes, builders, resets and `Eventually`; never sleep, never hardcode ids, never obtain tokens from a
login endpoint.

**Done when** each new behaviour has one test that fails without the change and passes with it, the
authorization matrix covers 401/403 for every new protected route, and the new code is inside the coverage
thresholds.

### Phase 8 — Gates

Run, in this order, and report real results:

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx
npm run verify
npm run coverage
```

On Windows use `win-x64` for the publish. If Docker is unavailable, say exactly which test projects could
not run; never claim they passed.

**Done when** build and AOT publish report zero warnings, every test passes, and every coverage threshold is
met. A warning is a failure (`TreatWarningsAsErrors`).

### Phase 9 — Report

Report what changed, the behaviour, the tests, the real command results and any follow-up. Use the template
at the end of this file.

## Reference map

| File | Read it for |
|---|---|
| `references/architecture.md` | Layer responsibilities, dependency rule, request lifecycle, patterns and their reasons, "where does this code go?" |
| `references/conventions.md` | Naming law, folder map, type shape, file-per-type, C# style, routes, SQL and test naming, commit convention |
| `references/feature-slice.md` | The ordered touch list for a new feature or endpoint, and removing the sample |
| `references/api.md` | Modules, `AddUseCase`, `IGroupMap`, the endpoints adapter, Result→HTTP, ProblemDetails, OpenAPI, JSON source generation, status codes |
| `references/persistence.md` | `NpgsqlDataSource`, ADO.NET repository, SQL scripts, seed, parameterisation, sort catalog + column map, unique index, test repository |
| `references/configuration-and-secrets.md` | `AppSettings`, `SectionBase`, validators, OpenBao, AppHost, precedence, test contract |
| `references/security.md` | JWT modes, policies, claims, `OwnerScope`, 401 vs 403, test identities |
| `references/testing.md` | What each test project covers, fixtures, attributes, builders, resets, coverage thresholds |
| `references/test-catalog.md` | What each existing test class protects, and which suite to run for each kind of change |
| `references/aspire.md` | AppHost topology, modes, `db-init`, `openbao-init`, devtoken, dashboard |
| `references/local-swagger.md` | The local loop, the Swagger sidecar, the examples catalog contract |
| `references/native-aot.md` | AOT properties, banned APIs, source-generated JSON, common warnings and fixes, publish gate |
| `references/quality-gates.md` | Commands, format, analyzers, tests, coverage, hooks, commit convention, troubleshooting |
| `references/review-checklist.md` | A diff review rubric with hard rules and failure signals |
| `references/glossary.md` | Technical vocabulary and the decisions behind the patterns |
| `references/sources.md` | Official documentation and tested versions |

## Worked example

`assets/examples/` contains an end-to-end `Project` feature: domain, application (create, read, filter, update,
delete and titles), infrastructure, API module, database scripts, tests and local OpenAPI examples. It mirrors
the repository layout and uses `CleanWebApiTemplate` names, so `dotnet new` rewrites them too. Copy the files in
layer order, keep the names and replace `Project`/`project` with the real feature.

Use the example when the repository's `Todo` sample is absent, has been modified, or when you need an
independent reference. Do not copy `Todo` from the template repository into the generated project.

## Report template

```markdown
## Summary
One paragraph: what the user can do now that they could not do before.

## Changes
- `path`: what changed and why.
- `path`: ...

## Behaviour
Endpoints, status codes, validation, ownership and schema changes. Mention any contract change.

## Tests
- Added/changed: project, class, what each proves.
- Commands run and real results (pass/fail counts, warnings, publish result).

## Gates
Build: ... | AOT publish: ... | Tests: ... | Coverage: ... | Verify: ...

## Follow-ups
Anything not done, not run, or deliberately left out (with the reason).
```

Never report a command as passed unless it ran and exited 0. Never hide a warning behind `tail` or `grep`.
