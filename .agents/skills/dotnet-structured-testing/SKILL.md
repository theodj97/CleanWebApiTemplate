---
name: dotnet-structured-testing
description: >-
  Use whenever the user wants automated tests in a .NET / C# solution: writing, adding, extending or fixing unit,
  component or functional (end-to-end) tests, testing an endpoint, covering a case or scenario, testing authorization
  by role, verifying published messages, or setting up or completing the test infrastructure (test projects, xUnit,
  Aspire test AppHost, Docker containers, Respawn, WireMock, test users and JWT tokens). Use it even if the user never
  mentions this skill, Aspire or xUnit, e.g. "add tests for POST /orders", "cover the case where the payment fails",
  "set up integration tests with a real database", and Spanish requests such as "añade tests", "testea este
  endpoint", "cubre este caso", "tests unitarios, de componente o funcionales", "monta la infraestructura de pruebas".
compatibility: Needs the .NET SDK used by the target repository. Component and functional tests need Docker or Podman; unit tests need neither.
---

# Structured .NET testing with xUnit and Aspire

This skill builds and extends a test suite with three clearly separated test types on a .NET repository you have
never seen:

- **Unit tests**: one class in isolation, dependencies substituted, no I/O.
- **Component tests**: one deployable service in-process, against real infrastructure in containers.
- **Functional tests**: business flows across the whole running system, as a black box.

Principles behind every rule below:

- **Real infrastructure, simulated third parties.** Databases, brokers and caches run as containers orchestrated by
  Aspire, with the same engine as production; only third-party HTTP/gRPC APIs are simulated (WireMock). Substitutes
  such as SQLite or in-memory transports behave differently and hide real bugs.
- **Discover, never assume.** Engine, configuration keys, schema mechanism, roles, versions and conventions come from
  the repository (`references/discovery.md`). A wrong assumption produces tests that pass against the wrong thing.
- **Aspire is only the orchestrator.** A test-only AppHost starts the environment; no dashboard, no telemetry.
- **Production code is not yours to change** — except minimal, declared testability changes (see Boundaries).

## Choose the mode

| Situation | Mode |
|---|---|
| No test projects, or the user asks to set up / build / complete the test infrastructure | **Setup** |
| The user asks for tests of a behaviour (endpoint, case, flow, class) and the infrastructure for that test type exists | **Incremental** |
| Incremental request, but the requested test type has no project/fixture yet (e.g. functional tests requested, only component tests exist) | **Partial setup** of that piece only — announce it first — then Incremental |

**Setup** creates or completes projects, AppHost, fixtures, base classes, attributes, users, resetters and helpers,
plus a few smoke tests per project. Complete means add what is missing; do not rewrite what exists and works.

**Incremental** never regenerates infrastructure. Reuse the existing fixture, base classes, repositories, builders and
helpers. Allowed changes are additive: a new test class, a builder, a test repository (and its fixture property) or
method, a `TestUser` value, a stub helper, a topic or configuration key in `TestHostContract`. A new infrastructure
dependency (a container the AppHost does not have yet) is a small, announced change to the AppHost and fixtures, not a
rebuild.

**Existing tests with other conventions** (another framework, Testcontainers, different naming or structure): in
Incremental mode follow what exists, even where it differs from this skill. In Setup mode propose how the new
structure fits next to it and ask before restructuring anything. Never migrate xUnit or other packages unless asked.

## Workflow

**Step 0 — Prerequisites.** Check the container runtime (`docker info` or `podman info`) and SDKs
(`dotnet --list-sdks`, `global.json`). Aspire uses Docker unless `ASPIRE_CONTAINER_RUNTIME=podman` is set: on a
Podman-only machine, tell the user to set it (and document it in `tests/README.md`). If no runtime is available, say so
immediately: unit tests can still be written and run; component and functional tests can be written and built but not
run.

**Step 1 — Discovery (always, before generating anything).** Follow `references/discovery.md`.
- Setup: present the full report (deployable apps, external dependencies, schema strategy and seed data, configuration
  keys, authentication/authorization, existing tests, solution format, decisions and questions) and **wait for the
  user's confirmation**. Ask before generating when something is ambiguous: no way to rebuild the schema, unclear
  roles, a hard-coded URL, an auth strategy that would need a production change.
- Incremental: a short report (what will be reused, what will be added, the behaviour found) and continue unless
  something blocks.

**Step 2 — Build or extend the infrastructure** (Setup / partial setup). Read these before adapting the templates:
1. projects, packages, solution folder, test AppHost — `references/project-setup.md`;
2. fixtures, base classes, serial execution — `references/fixtures.md`;
3. schema, resetters, test repositories, builders — `references/database.md`;
4. test users, token issuer, trust — `references/auth.md`;
5. brokers and third parties — `references/messaging.md`, `references/external-services.md`.

The Shared templates depend on each other (the fixture base uses auth, data, database and messaging helpers): copy and
adapt all of a project's templates, then build that project. Order: UnitTests → AppHost → Shared → ComponentTests →
FunctionalTests.

**Step 3 — Write the tests** with the conventions below and the reference of the test type:
`references/unit-tests.md`, `references/component-tests.md`, `references/functional-tests.md`.

**Step 4 — Verify** (see Verification) and report.

## Reference map

| Read | When |
|---|---|
| `references/discovery.md` | Always first: what to inspect, signals to look for, report templates |
| `references/project-setup.md` | Creating projects, choosing xUnit packages and versions, CPM, solution, test AppHost |
| `references/fixtures.md` | Fixture lifecycle and startup order, base classes, per-test resets, serial execution, xUnit v2 |
| `references/database.md` | Schema mechanism, `[ResetDatabase]`, Respawn per engine, surviving data, repositories, builders |
| `references/auth.md` | `TestUser`, `TestTokenIssuer`, making the app trust test tokens, Microsoft.Identity.Web, cookies/API keys |
| `references/messaging.md` | Kafka, RabbitMQ, Service Bus, SQS; verifying and publishing messages; messaging libraries |
| `references/external-services.md` | Substitute for each dependency, WireMock usage, MailPit, "nothing leaves the machine" |
| `references/unit-tests.md` | Writing unit tests |
| `references/component-tests.md` | Writing component tests, the factory, covering an endpoint, DI replacements, time |
| `references/functional-tests.md` | Writing functional tests, flow structure, several apps |
| `assets/README.md` | Which template goes where, placeholders, what to adapt |

## Target layout

```
tests/
├── orchestration/
│   ├── {App}.Tests.AppHost/   # test-only Aspire AppHost (Component | Functional mode)
│   └── database/              # schema or seed scripts, only when needed
├── {App}.UnitTests/
├── {App}.ComponentTests/
├── {App}.FunctionalTests/
├── {App}.Tests.Shared/        # code shared by component and functional tests
└── README.md                  # how to run each type, requirements
```

- One Unit/Component/Functional trio per deployable application (API, worker, function app). With several apps keep a
  single AppHost and a single Shared project named after the product.
- Add every project, the AppHost included, to the main solution inside a `tests` solution folder.
- Shared contains: the fixture base and test base classes; the `[ResetDatabase]`, `[AuthenticatedAs]` and
  `[Unauthenticated]` attributes; `TestUser`, `TestUsers` and `TestTokenIssuer`; the database resetters; test
  repositories and builders; the waiting helper (`Eventually`) and messaging helpers.

## What each project tests

| | Unit | Component | Functional |
|---|---|---|---|
| Subject | one class or a small cohesive group | one deployable service | a business flow across the system |
| Runs | in parallel, milliseconds, no Docker | serially, in-process `WebApplicationFactory<Program>` + Aspire infrastructure | serially, everything as Aspire resources |
| Dependencies | NSubstitute substitutes | real containers; third parties in WireMock; DI replacement only when a scenario cannot be provoked otherwise | real containers; third parties in WireMock; no DI access |
| Enters through | method calls | HTTP endpoints, message consumers | HTTP and messages only (black box); Arrange may seed the database |
| Verifies | return values, state, interactions | response, database, published messages, third-party calls | flow outcomes: responses, messages, eventual state |
| Amount | as needed; skip trivial code (DTOs, properties) and the framework | most integration tests live here | few, complete journeys |

Where a test belongs: pure logic → unit; anything that needs the database, the broker, the HTTP pipeline,
serialization, authorization or a third-party contract of one service → component; a journey that crosses services or
depends on the deployed composition → functional.

## Writing tests

Every test has three sections, marked and in this order:

```csharp
// Arrange   — state: repositories, WireMock stubs, messages to publish, the client of a user, or NSubstitute setups
// Act       — exactly one action: a method call, an HTTP request or a published message
// Assert    — status code and body, returned value, database state, broker messages, calls received by WireMock
```

- **Expected exceptions** keep the three sections: the Act captures the call as a delegate
  (`var act = () => sut.RunAsync(...)`) and the Assert verifies it (`await Assert.ThrowsAsync<T>(act)`).
- **One behaviour per test**; `[Theory]` for data variations of the same behaviour.
- **Names** `Method_Scenario_ExpectedResult`, unless the repository already uses another convention.
- **Folders mirror production code**; one test class per production class or endpoint group.
- Pass `CancellationToken` (the test base property, from `TestContext.Current`) to every async call that accepts one.

Reference example:

```csharp
public class OrdersEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.Customer)]
    public async Task CreateOrder_WithValidCustomer_PersistsOrderAndPublishesEvent()
    {
        // Arrange
        var customer = await Fixture.Customers.AddAsync(new CustomerBuilder().Build());
        var request = new CreateOrderRequest(customer.Id, Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await Fixture.Orders.GetSingleByCustomerAsync(customer.Id);
        Assert.Equal(100m, order.Amount);
        var published = await Fixture.Kafka.WaitForMessageAsync<OrderCreated>("orders", m => m.OrderId == order.Id);
        Assert.NotNull(published);
    }

    [Theory]
    [ResetDatabase]
    [InlineData(TestUser.Admin, HttpStatusCode.NoContent)]
    [InlineData(TestUser.Customer, HttpStatusCode.Forbidden)]
    public async Task DeleteOrder_ByUserRole_ReturnsExpectedStatus(TestUser user, HttpStatusCode expected)
    {
        // Arrange
        var order = await Fixture.Orders.AddAsync(new OrderBuilder().Build());
        var client = CreateClient(user);

        // Act
        var response = await client.DeleteAsync($"/orders/{order.Id}", CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }
}
```

### Identity

- `[AuthenticatedAs(TestUser.X)]` on the method or class sets the identity of `Client`; the method attribute wins.
- `[Unauthenticated]` cancels a class-level `[AuthenticatedAs]` for one test.
- No attribute → anonymous `Client`, so who makes each call is always visible in the test.
- `CreateClient(user)` in Arrange when the identity varies per `[Theory]` row (attribute values cannot change per
  `InlineData` row) or when a test needs several identities (A creates, B tries to access).
- `TestUser` is an enum generated from the roles and policies found: one user per role, users for the role/claim
  combinations policies require, and an authenticated user without roles for 403 cases. Details: `references/auth.md`.

### Database state

- One database per engine per run, created when the fixture starts; never a database per test — between tests only
  data is deleted (Respawn, or a custom resetter for engines Respawn does not support). Custom resetters delete only
  allowlisted collections/key prefixes and refuse non-local hosts.
- `[ResetDatabase]` resets every database; `[ResetDatabase("OrdersDb")]` only the named ones; `ExcludeTables` keeps extra
  tables for that test. On method or class; the method attribute wins.
- The attribute only declares intent; the test base class performs the reset asynchronously in `InitializeAsync`,
  because xUnit attribute hooks are synchronous and blocking async calls inside xUnit is a known deadlock source.
- Reset **before** each test, never after: each test starts from a known state even if the previous one was
  interrupted, and a failed test keeps its data for inspection.
- Tests never depend on concrete id values: use the ids the test repositories return.

### Test data

- One test repository per aggregate/table in Shared (`AddAsync` returns the entity with its id; query methods for Assert).
- Builders with valid defaults, so each test only declares what it cares about.
- Bogus always with a fixed seed (`TestData`), so failures reproduce with the same data.

### Asynchrony and determinism

- Eventual effects: `Eventually.AssertAsync(...)` / `WaitFor...` helpers — polling with a timeout. Never `Thread.Sleep`
  or a fixed `Task.Delay`: too short on a slow agent, wasted time everywhere else.
- Messages: helpers consume with a unique group id and only see messages produced after the test started; if the
  Arrange section itself makes the app publish, take a new snapshot (`Fixture.Kafka.CaptureOffsetsAsync()`) right
  before the Act.
- Time: `FakeTimeProvider` when the app uses `TimeProvider`; if it does not, report it as an improvement instead of
  changing production code.
- Component tests: disable `BackgroundService`s that interfere and are not under test.

## Packages and versions

- .NET: the repository's (`TargetFramework`, `global.json`).
- Always respect the libraries and versions the repository already uses. Defaults when there is nothing:
  NSubstitute (mocks), xUnit's own `Assert` (no extra assertion library), Bogus (data), Respawn (database reset),
  WireMock.Net (third-party APIs).
- xUnit v3 when there are no tests; existing tests keep their xUnit version. The exact xUnit v3 package set depends on
  how `dotnet test` runs in the repository (VSTest or Microsoft Testing Platform): see
  `references/project-setup.md`.
- New packages: the latest stable version compatible with the TFM, resolved at run time (`dotnet package search`),
  aligned with their family (ASP.NET Core / EF Core / Extensions with the app's major; all Aspire packages with one
  Aspire version — the repository's if it already uses Aspire).
- `Directory.Packages.props` / `Directory.Build.props`: add versions there, following their style.
- Never add ServiceDefaults or OpenTelemetry to the test environment, nor SQLite/EF InMemory providers.

## Boundaries

- Do not modify production code beyond what is strictly needed to test it, and always say so. Typical cases:
  `public partial class Program;` (only .NET 8/9; .NET 10 generates it), `InternalsVisibleTo`.
- Never change production code to make a test pass. If a test reveals a bug, report it with the failing test.
- No real secrets: signing keys and passwords are generated per run or come from local containers.
- Nothing leaves the machine: every URL of a real service is overridden, and safety nets make a missed one fail fast
  (`references/external-services.md`).

## Verification

At the end of either mode:

1. `dotnet build` of the solution: no errors (and no new warnings if the repository treats warnings as errors).
2. `dotnet test`: green. Unit tests must pass without Docker; component and functional tests with Docker/Podman. If
   Docker is not available, say exactly which tests could not be run.
3. Prove at least one new test can fail: change its expected value, run it, see it fail, revert the change, run it again.
4. Summary to the user:
   - what was created or changed (projects, files, production-code changes and why);
   - decisions, including the authentication strategy for component and functional tests;
   - assumptions (inferred schema, environment name, wire formats...);
   - what is pending (questions, tests not run, improvements found such as missing `TimeProvider`, bugs found).

## Common mistakes

| Mistake | Instead |
|---|---|
| Resetting the database after each test | Reset before; failed tests keep their data |
| A database or container per test/class | One environment per test project; reset data only |
| SQLite / EF InMemory / in-memory broker as a substitute | Same engine/broker in a container |
| `Thread.Sleep` / `Task.Delay` to wait for effects | `Eventually` and `WaitFor...` helpers with timeouts |
| Component/functional tests running in parallel | `xunit.runner.json` with `parallelizeTestCollections: false` |
| Reading "the last message" of a topic | Offset snapshot / probe bound before the Act + predicate on the test's ids |
| Async work blocked inside xUnit attributes (v3) | Attributes declare; the base class awaits |
| JWT `PostConfigure` that leaves a remote `ConfigurationManager` | Set `Configuration` and a `StaticConfigurationManager` in `Configure` and `PostConfigure` |
| Hard-coded ids (`/orders/1`) | Ids returned by test repositories |
| Regenerating fixtures or the AppHost in Incremental mode | Reuse; only additive changes |
| A real URL left in configuration | Override every URL; keep the safety nets on |
| Changing production code so a test passes | Report the bug |
| Roles as strings in tests | `TestUser` enum values |
| Development AppHost reused for tests | Dedicated test AppHost without dashboard, volumes or telemetry |
