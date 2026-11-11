# Testing

The template has four test projects plus `Tests.Shared`. Each answers a different question; putting a test in
the wrong project makes the suite slower without making it stronger. For the class-by-class map of the existing
suite, see `test-catalog.md`.

## Test map

| Project | Question it answers | Infrastructure | Docker | Parallel |
|---|---|---|---|---|
| `{App}.Api.UnitTests` | Does this pure logic/validator/handler make the right decision? | Substitutes (NSubstitute); no ASP.NET host | No | Yes |
| `{App}.Api.ComponentTests` | Does the API behave correctly through the full ASP.NET pipeline and the real database? | `WebApplicationFactory<Program>` in-process + shared AppHost (PostgreSQL, OpenBao) | Yes | No (shared database) |
| `{App}.Api.FunctionalTests` | Does a complete user journey work through real HTTP against a separately running process? | Shared AppHost in `Functional` mode; HTTP only, no service access | Yes | No (shared database) |
| `{App}.ArchitectureTests` | Do the dependency and structure rules still hold? | ArchUnitNET over the built assemblies | No | Yes |
| `{App}.Tests.Shared` | Fixtures, attributes, identities, token issuer, resetters, builders and helpers used by the other projects. | — | — | — |

Tests for `local/openapi/` live under `local/openapi/`, next to the sidecar. Tests of `src/` live under
`tests/`.

## Unit tests

Use for:

- validators: required, empty/whitespace, max length, email, ULID, date range, status, paging, sort keys;
- handlers with a substituted port: uniqueness, not-found, conflict propagation, trimming, audit fields,
  patch/no-op behaviour;
- `ValidatorSet<T>`: an empty set throws; validators aggregate their failures;
- `OwnerScope`: `VisibleOwner`, `OwnerForCreate` and `FilterOwners` for admins and non-admins;
- sort alignment: `{Feature}SortCatalog.KeySet` equals `{Feature}SortColumns.Map.Keys`;
- `Result<T>`/`Error` behaviour;
- pure helpers (`TodoTable`, OpenAPI schema helpers, configuration validation, secrets client with a fake
  handler);
- `ServiceDefaults` extensions that can be tested without a host.

Pattern:

```csharp
public class CreateProjectHandlerTests
{
    private const string Owner = "user@example.com";
    private readonly IProjectRepository _repository = Substitute.For<IProjectRepository>();
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _handler = new CreateProjectHandler(
            _repository,
            new ValidatorSet<CreateProjectInput>([new CreateProjectInputValidator()]));
    }

    [Fact]
    public async Task HandleAsync_WithNameAlreadyUsedByOwner_ReturnsConflictWithoutInserting()
    {
        // Arrange
        _repository.NameExistsAsync("Taken", Owner, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        Result<ProjectDto?> result = await _handler.HandleAsync(
            new CreateProjectInput { Name = "Taken", CreatedBy = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().InsertAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>());
    }
}
```

Rules:

- Pass `TestContext.Current.CancellationToken` (or the test's `CancellationToken` in component/functional
  tests).
- Do not mock the type under test; substitute the ports.
- Build the handler with a real `ValidatorSet<TInput>` (usually the feature's validator). An empty set is a
  composition error and throws by design; do not use `[]` to skip validation.
- Cover the `ValidatorSet` failure path directly (empty set, failing validator) in a dedicated test.
- Do not use a database, a container or an `HttpClient` here.

## Component tests

Use for:

- endpoint contract: status codes, headers, body shape, ProblemDetails;
- validation: 400 for each rule, 409 for duplicates;
- persistence: the row is stored with the right owner/audit fields;
- ownership: non-admin cannot read/update/delete another owner's row; admin can;
- authorization matrix: 401/403/2xx per route and role;
- OpenAPI document: operation ids, status codes, schema constraints;
- repository behaviour that needs PostgreSQL (unique index, filtering, sorting, paging).

Structure:

```csharp
public class ProjectEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithValidRequest_PersistsProjectAndCreatedBy()
    {
        // Arrange
        string name = TestData.Unique("project");

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/", new { name, description = "Created by a component test" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ProjectResponse? body = await response.Content.ReadFromJsonAsync<ProjectResponse>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        ProjectEntity? stored = await Fixture.Projects.FindByTitleAsync(name);
        Assert.NotNull(stored);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored!.CreatedBy);
    }
}
```

Component rules:

- The fixture starts the shared AppHost in `Component` mode: infrastructure and schema only; the API runs
  in-process.
- `Client` is built by `TestBase` from `[AuthenticatedAs]`/`[Unauthenticated]`; use `CreateClient(TestUser.X)`
  when the identity changes per `[Theory]` row or a test needs two identities.
- Use `Fixture.Projects` for arrange/assert, not raw SQL and not the HTTP API.
- `[ResetDatabase]` before a write; the reset happens before the test, so a failed test leaves its data.
- `CreateClientWithServices(...)` builds a second application instance for a scenario that cannot be provoked
  through the public entry points; use it sparingly.
- `BlockExternalHttpHandler` fails a non-local `HttpClient` call; do not relax it.

## Functional tests

Use for a complete journey that crosses several endpoints or a flow with several identities. Black box: HTTP
only, no `Fixture.Services`, no repository access for arrange (use HTTP or the seeded state).

```csharp
[Fact]
[ResetDatabase]
[AuthenticatedAs(TestUser.TodoOperator)]
public async Task ProjectLifecycle_CreateReadFilter_Succeeds()
{
    // Act - create
    HttpResponseMessage create = await Client.PostAsJsonAsync(
        "/api/Project/", new { name = TestData.Unique("flow") }, CancellationToken);
    Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    // ... read, filter
}
```

Rules:

- The fixture starts the app as a separate process under the AppHost (`Functional` mode) and waits for
  `/health/api`.
- `OutboundCalls.Requests` must stay empty; assert it at the end of a flow when the feature adds an external
  dependency.
- Use `CreateClient(TestUser.X, resourceName)` for a flow spanning several applications.
- No access to the application's DI container; that is what component tests are for.

## Architecture tests

`DependencyRulesTests` uses ArchUnitNET:

- Domain does not depend on Application/Infrastructure/Api/ServiceDefaults.
- Application does not depend on Infrastructure/Api/ServiceDefaults.
- Infrastructure does not depend on Api/ServiceDefaults.
- Application and Domain assemblies do not reference `Microsoft.AspNetCore*`.
- Application assembly does not reference Infrastructure.

`StructureTests` uses plain reflection:

- `I{Feature}Repository` and `{Feature}Filter` live in Application.
- `Result<T>` lives in `Application.Results`, not Domain.
- Classes under `Infrastructure.Repositories` end in `Repository` or live under a `Helpers` namespace.

**Known limitation:** the template's `StructureTests` assertion hardcodes
`Infrastructure.Repositories.Todo.Helpers`. Adding a second feature's helpers makes it fail
(`ProjectSortColumns is not a repository or a repository helper`). The `ProjectSortCatalog` lives in
Application, so it is not affected by this condition; only the Infrastructure `ProjectSortColumns` is.
Generalize the condition in the same change:

```csharp
|| type.Namespace!.Contains(".Helpers", StringComparison.Ordinal),
```

or add the new feature's prefix. Do not put the helpers in a non-`Helpers` namespace to work around it.

When a feature adds a port/filter or a repository helper, add an assertion in `StructureTests`. When a feature
adds a project or namespace, add it to the ArchUnitNET load list and a rule to `DependencyRulesTests`.

## Tests.Shared

| File | Purpose |
|---|---|
| `Fixtures/TestBase.cs` | Per-test lifecycle: reset, seed, `Client` from attributes, cancellation, client disposal |
| `Fixtures/TestServerFixtureBase.cs` | One AppHost per test project, schema/secrets initializers, connection string, test repository, resetters, readiness |
| `Fixtures/TestCancellation.cs` | The running test's cancellation token for helpers |
| `Attributes/AuthenticatedAsAttribute.cs` | `Client` identity on a class or method |
| `Attributes/UnauthenticatedAttribute.cs` | Force anonymous `Client` over a class identity |
| `Attributes/ResetDatabaseAttribute.cs` | Reset before the test, optionally named databases/extra tables |
| `Auth/TestUser.cs`, `TestUsers.cs` | One identity per role plus negative cases; fixed emails/subjects |
| `Auth/TestTokenIssuer.cs` | New HS256 key per run; valid and deliberately invalid tokens; AppHost arguments/settings |
| `Auth/TestAuthSettings.cs` | Audience/claim types per application; issuer and lifetime |
| `Data/TestData.cs` | Deterministic seed per test name; `Unique(prefix)`; `CreateFaker()` |
| `Data/{Feature}Builder.cs` | Valid defaults; only the test-specific fields are set |
| `Data/{Feature}TestRepository.cs` | Arrange/assert through the real repository |
| `Database/DatabaseResetConfiguration.cs` | Logical database names and tables that survive a reset |
| `Database/DatabaseResetters.cs` | Registry by logical name; `[ResetDatabase("...")]` resolution |
| `Database/RespawnDatabaseResetter.cs` | Respawn over PostgreSQL; created after the schema exists |
| `Waiting/Eventually.cs` | Poll an assertion/probe with a timeout; never sleep |
| `Http/OutboundCallRecorder.cs` | Proxy that records and refuses outbound calls in functional mode |

## Identities and ownership

- Every protected route gets a matrix: anonymous `401`, authenticated without roles `403`, wrong role `403`,
  allowed role `2xx`; plus a second identity for owner-scoped success/failure.
- `TodoUser` can read/create/update own rows and gets `403` on delete; `TodoOperator` and `TodoAdmin` can
  delete; `TodoExternal` is rejected on Todo routes; `AuthenticatedWithoutRoles` and
  `AuthenticatedWithoutEmail` are rejected everywhere.
- A non-admin must not see another owner's row; an admin must. Seed with `ProjectBuilder().CreatedBy(email)`.
- Never hardcode a token or copy one from a login endpoint. `TestTokenIssuer` signs per run.

## Data rules

- `TestData.Unique("project")` for unique columns; `TestData.CreateFaker()` for deterministic random text.
- `ProjectBuilder` supplies valid defaults and `CreatedBy`; build only the row the test needs.
- Never assert a concrete id; use the id returned by the API/seed.
- Dates: use `DateTime.UtcNow` in builders and compare with tolerance or `Eventually` for server-generated
  values.
- `[ResetDatabase]` before every test that writes; `[ResetDatabase("TodoDb")]` only when several databases
  exist. A reset never touches the schema.
- Keep reference/migration tables in `DatabaseResetConfiguration.TablesToIgnore`; everything tests write is
  reset.

## Waiting

Use `Eventually.AssertAsync(() => ...)` or `Eventually.GetAsync(probe, until)` for background effects and
startup. Never `Thread.Sleep` or a fixed `Task.Delay`. The timeout defaults to 10 seconds; pass a longer one
for a cold start (the fixture already allows five minutes for image pulls).

## Coverage

`npm run coverage` collects unit, component and functional coverage, merges it and writes
`coverage-report/coverage/report/`. Only `src/` assemblies are measured. Thresholds:

| Scope | Lines | Branches |
|---|---:|---:|
| `src/` total | 90% | 80% |
| `Api` | 85% | 75% |
| `Application` | 95% | 90% |
| `Domain` | 100% | 100% |
| `Infrastructure` | 90% | 85% |
| `ServiceDefaults` | 95% | 100% |

A new feature normally lowers coverage until its tests are added. Do not lower a threshold; add the tests.
Architecture and OpenAPI tests do not count toward coverage unless they execute the measured assembly.

## Running

```bash
# Everything (unit, component, functional, architecture)
dotnet test --solution {App}.slnx

# One project (MTP requires --project, not a path)
dotnet test --project tests/{App}.Api.UnitTests/{App}.Api.UnitTests.csproj

# Coverage
npm run coverage
```

`global.json` selects the Microsoft Testing Platform runner, so `dotnet test <path>` is rejected; use
`--project` or `--solution`.

## Checklist

- [ ] Validator tests cover every rule and the null/empty edge cases.
- [ ] `ValidatorSet` tests cover the empty-set failure and aggregated violations.
- [ ] `OwnerScope` tests cover admin and non-admin `VisibleOwner`/`OwnerForCreate`/`FilterOwners`.
- [ ] `{Feature}SortCatalog` and `{Feature}SortColumns` alignment is tested.
- [ ] Handler tests cover success, each business failure and propagation from the port.
- [ ] Component tests cover success, 400, 404/204, 409, persistence and ProblemDetails.
- [ ] Authorization matrix covers every new protected route and every role.
- [ ] Ownership tests cover non-admin vs admin and another owner.
- [ ] Functional test covers the end-to-end journey when the feature spans endpoints.
- [ ] Architecture test asserts the new port/filter/repository location.
- [ ] OpenAPI contract test includes the new operation ids and status codes.
- [ ] Tests use `[ResetDatabase]`, `[AuthenticatedAs]`, `TestData`, builders and `Eventually`.
- [ ] One new test was shown to fail without the change, then pass with it.
