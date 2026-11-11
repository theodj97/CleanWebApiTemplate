---
name: clean-webapi
description: >-
  Works on ASP.NET Core 10 repositories generated from the Clean Web API Template: Clean Architecture layers
  (Api, Application, Domain, Infrastructure, ServiceDefaults), Minimal API route groups, FluentValidation inputs
  and handlers, Result/Error, ADO.NET/Npgsql repositories, PostgreSQL schema scripts, OpenBao secrets, JWT
  modes, role policies and owner scoping, Native AOT, Aspire local development, and the
  unit/component/functional/architecture test suite. Use when adding or changing a feature, endpoint, validation
  rule, SQL table or seed row, configuration key, secret, authorization policy or test; when choosing where code
  belongs or naming a type; when fixing an IL2xxx/IL3xxx AOT warning; when reviewing a diff against the
  repository conventions; when explaining the architecture or the test suite; or when removing the sample Todo
  feature. Also for "add a feature", "add an endpoint", "add a test", "change the schema", "add a role", "why
  does this build fail with IL2026", and "review this change".
---

# Clean Web API: architecture, conventions and tests

This skill is for repositories generated from **CleanWebApiTemplate**: one solution named `{App}` with five
runtime projects, a `local/` Aspire environment and four test projects.

`{App}` is the solution name and root namespace, for example `Contoso.Orders`. Paths below use `{App}`.
The template's sample feature is `Todo`; use it as a worked example when it still exists, but do not depend
on it: every pattern this skill needs is reproduced in `assets/examples/` as a second feature (`Project`).

## Non-negotiables

1. **Dependencies point inward.** `Api → Application → Domain`, `Api → Infrastructure → Application`,
   `Infrastructure → Application/Domain`. `Application` never references `Infrastructure` or ASP.NET Core;
   `Domain` references nothing. `ServiceDefaults` is shared runtime code. → `references/architecture.md`
2. **The API stays Native AOT.** No reflection on runtime paths, no `dynamic`, no reflection-based JSON, no
   Swashbuckle schema generation, no AOT/trimming suppressions. Fix `IL2xxx`/`IL3xxx` at the cause.
   → `references/native-aot.md`
3. **Everything is registered explicitly.** No assembly scanning: a new handler, validator, `ValidatorSet`,
   repository, route group or JSON contract is added to `ConfigureServices`, `EndpointExtension`,
   `AppJsonSerializerContext` and the OpenAPI descriptors by hand. → `references/api.md`
4. **Business outcomes are results.** Handlers and repositories return `Result<T>` with a typed `Error`;
   they do not throw for validation, not-found or conflict. `ApiResultExtensions` chooses the status and
   `ProblemDetailsFactory` builds the body, so there is one ProblemDetails interface.
   → `references/architecture.md` § Result and Error
5. **SQL is parameterised and column-allow-listed.** Table and column names come from `*Table` constants; values are
   bound parameters; sort keys resolve through the feature's `*SortCatalog` (accepted vocabulary) and
   `*SortColumns` (physical map); user input never reaches SQL text. → `references/persistence.md`
6. **Ownership is not input.** `createdBy` comes from the JWT email through `ICurrentUser.Owner`, an
   `OwnerScope` value type; never from a request body. `OwnerScope` decides `VisibleOwner`,
   `OwnerForCreate` and `FilterOwners`. Non-admins only see and change their own rows; admins see every
   owner. → `references/security.md`
7. **Every change ships with the right tests.** Unit for pure logic and validators, component for the HTTP
   pipeline and database state, functional for a complete flow, architecture for dependency and structure
   rules. → `references/testing.md`
8. **A change is done only when the gates are green.** Build with zero warnings, Native AOT publish with zero
   warnings, format, analyzers, every test and the coverage thresholds. A green build is not a green AOT
   publish. → `references/quality-gates.md`

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
| Start, stop, debug or extend the local environment | **Aspire** | `references/aspire.md`; use `aspire` for setup/repair and `aspire-orchestration` for start/stop |
| An `IL2xxx`/`IL3xxx` warning or an AOT publish failure | **AOT** | `references/native-aot.md` |
| Review a diff or the working tree | **Review** | `references/review-checklist.md`; use `code-review` for the full two-axis review |
| Explain the architecture, a pattern, a name or the test suite | **Explain** | `references/architecture.md`, `references/conventions.md`, `references/glossary.md`, `references/testing.md`, `references/test-catalog.md` |
| Remove the sample `Todo` feature | **Remove sample** | `references/feature-slice.md` § Removing the sample |
| A failing endpoint, test or local run | **Debug** | `references/quality-gates.md` § Troubleshooting, `references/aspire.md`, `references/native-aot.md`; use `diagnosing-bugs` when it resists a first glance |

## Main flow

The flow applies to **Feature**. **Endpoint**, **Schema**, **Config**, **Auth**, **Aspire** and **AOT** run the
phases their reference names and still end with Phase 8 (gates). **Test**, **Review**, **Explain**, **Remove
sample** and **Debug** follow their reference's completion criteria and run the gates only when they change
files. Read `AGENTS.md` and the repository README first; they win over this skill when they differ.

### Phase 0 — Read the repository

Identify the solution file, the five runtime projects, the test projects, the AppHost, the existing feature
closest to the change and its tests. Read `Directory.Build.props`, `Directory.Packages.props`, `global.json`,
`BannedSymbols.txt` and the API's `appsettings*.json`.

**Done when** you can name the layer and folder of every type you are about to add, and the exact files you
will touch, without editing anything.

### Phase 1 — Plan

Produce a short plan: endpoints and status codes, types per layer, schema and seed changes, registrations,
tests per project and docs. Present it and **wait for confirmation** when the change adds a table, a secret,
a policy, a project, or touches more than one layer in a way that changes the contract. Otherwise continue.

**Done when** every file in the plan has a named owner layer and the plan names the test that proves each
behaviour.

### Phase 2 — Domain

Add the entity, enum, constraints or value object. Domain has no framework references and no HTTP or
configuration concepts.

**Done when** the type compiles using only `System.*` and Domain types, and the business constraints live in
`Domain/Models/{Feature}/{Feature}Constraints.cs`, not in SQL or validators.

### Phase 3 — Application

Add the input, validator, port, filter, DTO, DTO mapping and handler. The handler implements
`IHandler<TInput, TResult>`, exposes `HandleAsync` and returns `Result<TResult>`. It takes a
`ValidatorSet<TInput>`, not a raw `IEnumerable<IValidator<TInput>>`; the set refuses an empty registration,
and `ValidateOnBuild` makes that a startup failure. Validation is input shape; uniqueness and other business
rules belong to the handler. If more than one handler needs the same behaviour, add a shared service interface
in `Application/Abstractions/Services`; keep one handler per use case.

**Done when** the handler has no ASP.NET, Npgsql or `IHttpContextAccessor` reference, every business failure
is a typed `Error`, the port is the only thing Infrastructure will implement, and every handler has a
registered `ValidatorSet<TInput>`.

### Phase 4 — Infrastructure and database

Implement the port with ADO.NET/Npgsql, a `{Feature}Table` helper, a `{Feature}SortCatalog` in Application
and a `{Feature}SortColumns` map in Infrastructure. Add the ordered SQL script and the seed. Handle the
unique index as a `ConflictError`.

**Done when** every value is a bound parameter, every column comes from a constant, the sort catalog and
the SQL map name the same keys (a unit test proves it), and the schema script applies from an empty database.

### Phase 5 — API

Add the request/response contracts, `ToInput` mapping, the `{Feature}Endpoints` adapter, the route group and
endpoint metadata. Register the validator, `ValidatorSet<T>`, handler, repository and route group explicitly;
add the request/response types to `AppJsonSerializerContext` and the schema descriptions to the OpenAPI
descriptors.

**Done when** every endpoint returns the documented status codes, every contract type is in the JSON context,
and the OpenAPI document names every operation and response.

### Phase 6 — Local environment

Add the seed rows, the OpenAPI examples, any devtoken profile or role, and any AppHost resource the feature
needs. A new table normally needs no AppHost change: `db-init` applies every `*.sql` script in order.

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
dotnet build {App}.slnx
dotnet publish src/{App}.Api/{App}.Api.csproj -c Release -r linux-x64 --self-contained
dotnet test --solution {App}.slnx
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
| `references/conventions.md` | Naming law, folder map, file-per-type, C# style, SQL and test naming, commit convention |
| `references/feature-slice.md` | The ordered touch list for a new feature or endpoint, and removing the sample |
| `references/api.md` | Route groups, `IGroupMap`, `{Feature}Endpoints` adapter, Result→HTTP, ProblemDetails, OpenAPI, JSON source generation, status codes |
| `references/persistence.md` | ADO.NET repository, SQL scripts, seed, parameterisation, `{Feature}SortCatalog` + `{Feature}SortColumns`, unique index, test repository |
| `references/configuration-and-secrets.md` | `AppSettings`, `SectionBase`, validators, OpenBao, AppHost, configuration precedence, test contract |
| `references/security.md` | JWT modes, policies, claims, `OwnerScope`, 401 vs 403, test identities |
| `references/testing.md` | What each test project covers, fixtures, attributes, builders, resets, coverage thresholds |
| `references/test-catalog.md` | What each existing test class protects, and which suite to run for each kind of change |
| `references/aspire.md` | AppHost topology, modes, `db-init`, `openbao-init`, Swagger sidecar, devtoken, dashboard |
| `references/native-aot.md` | AOT properties, banned APIs, source-generated JSON, common warnings and fixes, publish gate |
| `references/quality-gates.md` | Commands, format, analyzers, tests, coverage, hooks, commit convention, troubleshooting |
| `references/review-checklist.md` | A diff review rubric with hard rules and failure signals |
| `references/glossary.md` | Ubiquitous language and the decisions behind the patterns |
| `references/sources.md` | Official documentation and tested versions |

## Worked example

`assets/examples/` contains an end-to-end `Project` feature (create, read, filter): domain, application,
infrastructure, API, database, tests and local OpenAPI examples. It uses `{App}` placeholders and assumes the
template's shared types
(`BaseEntity`, `IHandler<TInput, TResult>`, `BaseAbstractValidator`, `ValidationService`, `ValidatorSet<T>`,
`Result<T>`, `Error`, `OwnerScope`, `ICurrentUser`, `IBaseResponse`) exist. Copy the files in layer order and
replace `Project`/`project` with the real feature. The example also uses a `ProjectSortCatalog`,
`ProjectEndpoints` and a `ProjectSortColumnsTests` alignment test.

Use the example when the repository's `Todo` sample is absent, has been modified, or when the user asks for an
independent reference. Do not copy the `Todo` feature from the template repository into the generated project.

## Companion skills

- **`aspire`** — setup, repair and debugging of the local environment and test infrastructure when the AppHost
  is missing, duplicated or not used by the tests; use `aspire-orchestration` for the start/stop lifecycle.
  This skill covers the template's own topology.
- **`code-review`** — the two-axis Standards + Spec review. Give it `references/review-checklist.md` and
  `references/conventions.md` as the standards sources.
- **`tdd`** — build the slice one red-green test at a time.
- **`diagnosing-bugs`** — use it for a failure that a first look does not explain.

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
