# Glossary and decisions

The vocabulary an agent should use when talking about this repository. The decisions are the "why" that the
code alone does not show; treat them as ADR summaries.

## Vocabulary

| Term | Meaning |
|---|---|
| **App** (`{App}`) | The solution name and root namespace, for example `Contoso.Orders`. |
| **Feature** | One business concept (`Project`, `Todo`) with its own slice across layers. Singular. |
| **Slice / vertical slice** | The files a feature touches in Domain, Application, Infrastructure, API, SQL and tests. |
| **Request** | The API body contract (`CreateProjectRequest`). It belongs to Api and maps to an Input. |
| **Input** | The application contract (`CreateProjectInput`). It belongs to Application and carries the owner/audit fields. |
| **Entity** | The Domain persistence model (`ProjectEntity`). Never leaves Application/Infrastructure. |
| **DTO** | The Application projection (`ProjectDto`). It crosses from Application to Api. |
| **Response** | The API public contract (`ProjectResponse`). It implements `IBaseResponse` and is the OpenAPI schema. |
| **Handler** | One Application use case (`CreateProjectHandler`). It implements `IHandler<TInput, TResult>` and exposes `HandleAsync`. |
| **Service** | A shared collaborator used by several handlers (`ITodoTitleService`; the template has none yet, so create it when a second handler needs the behaviour). Its interface lives in `Application/Abstractions/Services`; it is not a use case. |
| **Port** | An Application interface implemented by Infrastructure (`IProjectRepository`). |
| **Adapter** | An Infrastructure implementation of a port (`ProjectRepository`) or a configuration source. |
| **Repository** | The data-access adapter. It may return `Result<T>` for a constraint violation. |
| **Filter** | A persistence-neutral query shape (`ProjectFilter`); the API parses strings into it. |
| **Result** | `Result<T>`: success with a value or failure with an `Error`. |
| **Error** | A typed business failure (`NotFoundError`, `ConflictError`, ...) mapped to HTTP in Api. |
| **Owner scope** | `OwnerScope` (`ICurrentUser.Owner`): a value type with `VisibleOwner` (`null` for admins), `OwnerForCreate` (always the caller) and `FilterOwners(requested)`. The rule lives here, not in the HTTP mappers. |
| **Validation set** | `ValidatorSet<TInput>`: the validators for one input. It refuses an empty set, so a missing registration fails at startup. |
| **Sort catalog** | `{Feature}SortCatalog` (Application): the accepted sort keys and their description. |
| **Sort column map** | `{Feature}SortColumns` (Infrastructure): the catalog keys mapped to physical columns; a test asserts alignment. |
| **Projection** | A deliberately partial read type (`TodoTitle`) in `Application/{Feature}/Projections`; it crosses the port and handler without nullable fields or `!` casts. |
| **Endpoint adapter** | `{Feature}Endpoints` (Api): the request → input → handler → HTTP response plumbing, kept out of `{Feature}Routes`. |
| **Audit fields** | `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`. |
| **Policy** | An ASP.NET authorization policy (`User`, `Operator`, `Admin`, `External`) requiring a role and an email claim. |
| **Test user** | A fixed identity used by tests (`TodoUser`, `TodoOperator`, ...). One per role plus negative cases. |
| **Fixture** | An xUnit assembly fixture that starts the shared AppHost once per test project. |
| **Builder** | A test helper with valid defaults (`ProjectBuilder`). |
| **Resetter** | A Respawn-based helper that empties the database between tests. |
| **Local mode** | `aspire run` / `aspire start`: dashboard, telemetry, seed, Swagger sidecar. |
| **Component mode** | Tests: infrastructure + schema, API in-process. |
| **Functional mode** | Tests: infrastructure + schema + API as a separate process, HTTP only. |
| **AppHost contract** | `AppHostContract.cs`: run modes, resource names, config keys and JWT trust mapping shared by the AppHost and tests. |
| **Gate** | A command that must pass before a change is done (`build`, `publish`, `test`, `verify`, `coverage`). |
| **Source of truth** | The one place a fact lives: schema in `local/database`, table names in `{Feature}Table`, config in `AppSettings`, local topology in the AppHost. |

## Decisions

### D-001 — `Result<T>` and `Error` live in Application

**Decision:** Application handlers return `Result<T>`; the `Error` hierarchy is in `Application/Results`;
`ApiResultExtensions` chooses the status and `ProblemDetailsFactory` builds every ProblemDetails body.

**Reason:** business failures are expected outcomes, not exceptions; HTTP is an Api concern. Domain stays free
of transport concepts, and one factory keeps `type`/`traceId` consistent.

**Consequence:** a new error type needs a mapping and OpenAPI metadata in the same change. Domain must not
reference `Result<T>`.

### D-002 — No MediatR, no validation decorators, no pipeline

**Decision:** handlers inherit `ValidationService`, call `ExecuteAsync`, and receive a `ValidatorSet<TInput>`
through the constructor. `ValidatorSet` refuses an empty registration, and `Program.Main` enables
`ValidateOnBuild`.

**Reason:** explicit and AOT-safe. No assembly scanning, no dynamic dispatch, no hidden pipeline; a missing
registration is a startup failure, not a silent validation bypass.

**Consequence:** a new handler must be registered by its closed `IHandler<TInput, TResult>` interface and its
`ValidatorSet<TInput>` must be registered too. There is no "automatic" cross-cutting behaviour; add it to the
base class if it is truly shared.

### D-003 — ADO.NET/Npgsql instead of EF Core or Dapper

**Decision:** repositories use `NpgsqlCommand`/`NpgsqlDataReader` with manual mapping.

**Reason:** predictable SQL, no reflection/expression trees, Native AOT compatibility, no ORM model drift.

**Consequence:** every query and mapping is written by hand; `*Table`, `*SortCatalog`, `*SortColumns` and the
schema script are the sources of truth. Adding a sort key edits the catalog and the column map, and the
alignment test fails until both agree.

### D-004 — Explicit composition root

**Decision:** `ConfigureServices` registers every validator, `ValidatorSet<T>`, handler and repository;
`EndpointExtension` registers every route group.

**Reason:** the AOT analyzer and the reader both see every type; no assembly scanning, no magic.

**Consequence:** a forgotten registration fails at startup through `ValidateOnBuild`; the component tests
catch anything that only fails when a request is served.

### D-005 — One AppHost, three modes

**Decision:** `local/aspire/{App}.AppHost` serves Local, Component and Functional modes; there is no test-only
AppHost.

**Reason:** one topology, one contract, no drift between local development and tests.

**Consequence:** `AppHostContract` is shared with `Tests.Shared`; a test-mode change must not leak into Local
mode, and vice versa.

### D-006 — Ephemeral local infrastructure

**Decision:** no volumes, bind mounts or persistent lifetimes. `db-init` applies schema every start and seed in
Local mode; `openbao-init` writes secrets every start.

**Reason:** a start is reproducible and stopping removes the data; no stale local state.

**Consequence:** never rely on data surviving a restart; the seed must cover every state; a test gets an empty
schema.

### D-007 — Locally signed JWTs, no identity provider

**Decision:** local development and tests use HS256 tokens signed with a key in OpenBao/AppHost secrets; tests
sign their own tokens with a per-run key.

**Reason:** no external dependency, no login endpoint, validation stays on, tokens are isolated per run.

**Consequence:** `TestTokenIssuer`, `AppHostContract.JwtTrust` and `devtoken` must agree on issuer, audience,
key and claim types. Never obtain a test token from a login endpoint.

### D-008 — OpenBao for secrets

**Decision:** the application reads secrets from OpenBao at startup; `openbao-init` seeds them; `appsettings`
holds no secrets or connection strings.

**Reason:** the same code path in local, test and deployed environments; no secret in Git or logs.

**Consequence:** adding a secret means updating the initializer, the AppHost parameter, the docs and the tests
that read it; the application never receives the value as an environment variable.

### D-009 — Native AOT first

**Decision:** the API is published with Native AOT; no reflection, `dynamic`, reflection JSON, Swashbuckle or
suppressions.

**Reason:** startup, size and trimming safety; the template proves the code path works.

**Consequence:** every new contract is in the JSON context; every new registration is explicit; every new
package is checked for AOT compatibility; `dotnet publish` is the gate.

### D-010 — Four test levels

**Decision:** unit for pure logic, component for the HTTP pipeline + real database, functional for a complete
flow, architecture for dependency/structure rules.

**Reason:** each level answers a different question; the suite stays meaningful and as fast as the
infrastructure allows.

**Consequence:** a feature normally adds unit + component + functional (when it spans endpoints) + an
architecture assertion for a new port/filter; a component test is not a substitute for a unit test and vice
versa.

### D-011 — Sample Todo is a reference, not a dependency

**Decision:** the template ships a `Todo` sample; this skill ships a second, independent `Project` example.

**Reason:** an agent must be able to work after the sample is removed or changed; the example demonstrates the
same patterns without depending on the repository's sample files.

**Consequence:** use `../assets/examples/` when `Todo` is absent or modified. When removing `Todo`, follow the
ordered checklist and keep the `db-init` readiness query valid.

### D-012 — Handlers own use cases; services are shared collaborators

**Decision:** one handler per use case in `{Feature}/Handlers/{Verb}`. Behaviour reused by several handlers
gets an interface in `Application/Abstractions/Services`.

**Reason:** keeps use cases explicit and avoids a service layer that duplicates handlers.

**Consequence:** do not create a service for one handler; extract it when a second handler needs it.

### D-013 — OwnerScope is a value type, not a nullable string

**Decision:** `ICurrentUser.Owner` returns `OwnerScope`; the type owns `VisibleOwner`, `OwnerForCreate` and
`FilterOwners`. The HTTP request mappers call those methods; they do not implement the rule themselves.

**Reason:** one authorization rule, one place to test it. A nullable string hides the difference between
"admin can see every owner" and "owner is missing".

**Consequence:** new endpoints use `currentUser.Owner`; a new owner rule is added to `OwnerScope` and covered by
`OwnerScopeTests`, not copied into a mapper.

### D-014 — Partial reads use projection types; endpoint plumbing uses an adapter

**Decision:** a query that selects a subset returns a projection type from
`Application/{Feature}/Projections` (for example `TodoTitle`). The request → input → handler → response
plumbing lives in `{Feature}Endpoints`; `{Feature}Routes` keeps templates and metadata.

**Reason:** partial entities/DTOs push `!` casts and runtime surprises to the response edge; repeated endpoint
plumbing hides the convention in six copies.

**Consequence:** do not fill an all-nullable DTO and cast it later; add a projection. Do not put owner or result
mapping back in the route group; add an adapter method.
