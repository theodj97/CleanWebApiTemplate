# Testing

Four suites with different jobs, one shared fixture library, and a coverage gate. This reference explains
what to write where and which helpers already exist.

## Projects

| Project | Tests | Docker | Parallel |
|---|---|---|---|
| `CleanWebApiTemplate.UnitTests` | Pure logic: validators, handlers, Result/Error, OpenAPI helpers, configuration, secrets client, JSON contracts, sort alignment. Substitutes, no containers. | No | Yes |
| `CleanWebApiTemplate.Api.ComponentTests` | The API in-process (`WebApplicationFactory<Program>`) against the real PostgreSQL and OpenBao containers started by the shared AppHost in `Component` mode. Endpoints, validation, ownership, authorization, OpenAPI, repository. | Yes | No (one database) |
| `CleanWebApiTemplate.Api.FunctionalTests` | Complete flows with the API as a separate process under the shared AppHost in `Functional` mode. HTTP only. | Yes | No |
| `CleanWebApiTemplate.ArchitectureTests` | Dependency rules (ArchUnitNET) and folder/naming conventions. | No | Yes |
| `CleanWebApiTemplate.Tests.Shared` | Not a test project: fixtures, attributes, identities, tokens, builders, repositories, resets. | — | — |

`global.json` selects Microsoft Testing Platform, so `dotnet test` needs `--solution` or `--project`.

```bash
dotnet test --solution CleanWebApiTemplate.slnx
dotnet test --project tests/CleanWebApiTemplate.UnitTests/CleanWebApiTemplate.UnitTests.csproj
npm run coverage
```

## Test infrastructure

### Attributes

- `[AuthenticatedAs(TestUser.X)]` — the test's `Client` sends that identity's token. On a class it applies to
  every test; a method attribute wins.
- `[Unauthenticated]` — forces an anonymous client, overriding a class-level `[AuthenticatedAs]`.
- `[ResetDatabase]` — resets every registered database before the test; `[ResetDatabase("TodoDb")]` resets
  only that one; `ExcludeTables` keeps specific tables for that test. The reset runs **before** the test, so a
  failure leaves its data for inspection.

### `TestBase`

`tests/CleanWebApiTemplate.Tests.Shared/Fixtures/TestBase.cs` (component and functional) does four things
before each test:

1. `Fixture.PrepareForTestAsync(testName)` — reseeds `TestData` from the test name.
2. Applies `[ResetDatabase]` (method over class) and re-seeds surviving data.
3. Builds `Client` from `[AuthenticatedAs]`/`[Unauthenticated]` (anonymous by default).
4. Exposes `CancellationToken` (the xUnit test token) and `Fixture`.

Helpers: `CreateClient(TestUser)` (several identities in one test), `CreateAnonymousClient()`,
`CreateClientWithInvalidToken(InvalidToken, TestUser)`, `Authorize(client, user)`, `Track(client)`.

### Fixtures

`TestServerFixtureBase` starts the shared AppHost once per test project:

1. `DistributedApplicationTestingBuilder` with `RunMode=Component|Functional`, the OpenBao root token and the
   JWT trust arguments.
2. Waits for `postgres`, `postgresdb` and `openbao` to be healthy.
3. Waits for `db-init` and `openbao-init` to exit 0.
4. Builds the test repository over the Aspire connection string.
5. Derived fixture starts the application (in-process factory or explicit AppHost start).
6. Registers the Respawn resetters (after the schema exists).
7. Seeds persistent data and waits until the application answers `/health/api`.

`ComponentTestFixture` builds `ComponentAppFactory` with in-memory settings: log levels, JWT trust, OpenBao
address/token/mount/path. It asserts that `API_KEY` and `ConnectionStrings:Postgres` came from OpenBao and
that no `JwtBearer` scheme would download metadata. `ComponentAppFactory` uses the `Testing` environment,
`ValidateScopes`/`ValidateOnBuild` and adds `BlockExternalHttpHandler` to every `HttpClient`.

`FunctionalTestFixture` starts the API resource explicitly, waits for `/health/api`, and records outbound
calls in `OutboundCallRecorder` (the `HTTP(S)_PROXY` points at it). `OutboundCalls.Requests` must be empty.

`DatabaseResetters` + `RespawnDatabaseResetter` empty the tables before a test; Respawn reads the table graph
after the schema exists, so a new table is reset automatically. `DatabaseResetConfiguration.Names` holds the
logical names (`TodoDb`).

### Identities and tokens

`TestUsers` defines the fixed subject, name, roles and email of every `TestUser`; `TestTokenIssuer` signs a
new HS256 key per run and issues valid and invalid tokens (`InvalidToken`). The key is never written to disk;
the AppHost receives it as an argument and seeds it into OpenBao. Never add a login endpoint to the API for
tests.

### Data

- `TestData.BeginTest(testName)` seeds a deterministic `Faker` per test; `TestData.Unique(prefix)` produces a
  unique value for a unique constraint.
- `TodoBuilder` creates a valid entity with only the fields a test cares about.
- `TodoTestRepository` (`Fixture.Todos`) arranges rows and asserts stored state. It wraps the real repository;
  there is no raw SQL in tests.
- `Eventually.AssertAsync` / `Eventually.GetAsync` poll for eventual effects; never `Thread.Sleep` or a fixed
  `Task.Delay`.

## What to test where

| Behaviour | Suite |
|---|---|
| A validator rule, a handler branch, `Result`/`Error`, OpenAPI helper, JSON context, sort alignment | Unit |
| Status code, ProblemDetails shape, validation at the HTTP edge, ownership, role matrix, repository conflict, OpenAPI document | Component |
| A complete journey (create → read → update → delete) across processes | Functional |
| Layer dependencies, folder conventions, sealed handlers, infrastructure shape | Architecture |

Component test conventions:

- Derive from `ComponentTestBase`, take `ComponentTestFixture` in the constructor.
- `[ResetDatabase]` unless the test truly does not touch data.
- Use `Client` (identity from attributes) or `CreateClient(TestUser.X)` for a different identity.
- Assert status first, then the body, then the stored state through `Fixture.Todos`.
- An authorization matrix is a `[Theory]` over `(method, path, body)` for anonymous/403 and at least one
  2xx case per method.
- `Fixture.OutboundCalls` must be empty in functional tests.

## Coverage

`npm run coverage` builds, collects unit, component and functional coverage with `dotnet-coverage`, merges it
and writes `coverage-report/coverage/report/`. Architecture and sidecar tests are not collected. Thresholds:

| Scope | Lines | Branches |
|---|---:|---:|
| `src/` total | 90 % | 80 % |
| Api | 85 % | 75 % |
| Application | 95 % | 90 % |
| Domain | 100 % | 100 % |
| Infrastructure | 90 % | 85 % |
| ServiceDefaults | 95 % | 100 % |

Coverage is a floor, not a goal: do not add tests of constants to raise it. A new branch in `ApiResultExtensions`
or a new error type needs a test that exercises it.

## Checklist for a new feature

- [ ] Unit: one test class per validator and per handler (NSubstitute port, real `ValidatorSet`), including
      the conflict and not-found branches.
- [ ] Unit: sort alignment (`SortCatalog.KeySet` vs `SortColumns.Map.Keys`) and JSON context coverage.
- [ ] Unit: request mapping (`ToInput`) for the owner rules (admin vs non-admin).
- [ ] Component: create (201, trim, duplicate 409, different owner 201), get (200, 404 unknown/invisible),
      filter (200, empty array, owner scoping, sort, paging, contains), update (200, 404, 409, patch
      semantics), delete (204, 404, operator/admin only).
- [ ] Component: authorization matrix 401/403/2xx per protected route.
- [ ] Component: OpenAPI document has the operations and documented status codes.
- [ ] Functional: one lifecycle flow; the invalid-token matrix when auth changes.
- [ ] Architecture: nothing to edit when the folders follow the convention.
- [ ] Local examples updated (see `local-swagger.md`).

## Common failures

| Symptom | Cause |
|---|---|
| `The application did not read 'API_KEY'/'ConnectionStrings:Postgres' from OpenBao` | `openbao-init` did not write it, or the application got it from another source. |
| `The application does not read the test value for '<key>'` | The key name differs from what the app reads, or `Program` adds a source later. |
| 401 in every authenticated test | Issuer/audience/claim types do not match `TestAuthSettings` or the AppHost trust. |
| 403 where success was expected | The role or claim type does not match the policy. |
| `Blocked outbound HTTP call` | A dependency points at a real URL; map it to a local substitute. |
| `'db-init' ended … exit code 1` | The schema failed: reproduce with `aspire logs db-init`. |
| `CS0433: 'Program' exists in both …` | The AppHost reference must use `Aliases="apphost"` and `extern alias apphost;`. |
