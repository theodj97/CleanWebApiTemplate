# Testing with the shared AppHost: test types, conventions, fixtures, component and functional tests

Canonical for the test rules of SKILL.md (rules 4, 6, 8, 9, 12 as they apply to tests). Templates:
`assets/tests/shared/*`, `assets/tests/component/*`, `assets/tests/functional/*`, `assets/tests/xunit.runner.json`,
`assets/tests/tests-README.md`. Unit tests: `unit-tests.md`. Databases and resets: `database.md`. Tokens:
`local-jwt-authentication.md`. Brokers and third parties: `messaging.md`, `external-services.md`.

## Contents
- [Test types](#test-types)
- [Modes of test work](#modes-of-test-work)
- [Writing tests](#writing-tests)
- [Identity](#identity)
- [Asynchrony, time and determinism](#asynchrony-time-and-determinism)
- [The fixture: one environment per test project](#the-fixture-one-environment-per-test-project)
- [Startup order](#startup-order)
- [Component vs functional fixture](#component-vs-functional-fixture)
- [Test base classes](#test-base-classes)
- [Per-test reset of shared state](#per-test-reset-of-shared-state)
- [Serial execution and cancellation](#serial-execution-and-cancellation)
- [Component tests](#component-tests)
- [Functional tests](#functional-tests)
- [Startup failures and diagnostics](#startup-failures-and-diagnostics)
- [xUnit v2 variant](#xunit-v2-variant)
- [Common mistakes](#common-mistakes)

## Test types

| | Unit | Component | Functional |
|---|---|---|---|
| Subject | one class or a small cohesive group | one deployable service | a business flow across the system |
| Runs | in parallel, milliseconds, no Docker, no Aspire | serially, in-process `WebApplicationFactory<Program>` + AppHost in Component mode | serially, everything as Aspire resources, AppHost in Functional mode |
| Dependencies | NSubstitute substitutes | real containers; third parties in WireMock; DI replacement only when a scenario cannot be provoked otherwise | real containers; third parties in WireMock; no DI access |
| Enters through | method calls | HTTP endpoints, message consumers | real HTTP on dynamic ports and messages only (black box); Arrange may seed the database |
| Verifies | return values, state, interactions | response, database, published messages, third-party calls | flow outcomes: responses, messages, eventual state |
| Amount | as needed; skip trivial code and the framework | most integration tests live here | few, complete journeys |

Where a test belongs: pure logic → unit; anything that needs the database, the broker, the HTTP pipeline,
serialization, authentication/authorization or a third-party contract of one service → component; a journey that
crosses services or depends on the deployed composition, and the proof that each service trusts the local issuer out
of process → functional.

Component tests send requests through the complete ASP.NET Core pipeline (routing, authentication, authorization,
model binding, serialization) via `TestServer`, which keeps the transport in memory. They never call controllers or
handlers directly. Functional tests are the ones that use real sockets and Aspire's dynamic ports.

## Modes of test work

| Situation | Mode |
|---|---|
| No test projects, or the user asks to set up / complete the test infrastructure | **Setup** (part of `setup`/`repair`) |
| Tests for a behaviour (endpoint, case, flow, class) and the infrastructure for that test type exists | **Incremental** |
| Incremental request, but the requested test type has no project/fixture yet | **Partial setup** of that piece only, announced first, then Incremental |

- **Setup** creates or completes projects, fixtures, base classes, attributes, users, resetters and helpers, plus a few
  smoke tests per project (including the authentication matrix). Complete means add what is missing; do not rewrite
  what exists and works.
- **Incremental** never regenerates infrastructure. Reuse the existing fixture, base classes, repositories, builders
  and helpers. Allowed changes are additive: a test class, a builder, a test repository (and its fixture property) or
  method, a `TestUser` value, a stub helper, a resource name, topic or configuration key in `AppHostContract`. A new
  infrastructure dependency (a container the AppHost lacks) is a small, announced change to the AppHost and fixtures.
- **Existing tests with other conventions** (another framework, Testcontainers, different naming): in Incremental mode
  follow what exists, even where it differs from this skill. In Setup mode propose how the new structure fits next to
  it and ask before restructuring. Never migrate xUnit or other packages unless asked.

## Writing tests

Every test has three sections, marked and in this order:

```csharp
// Arrange   — state: repositories, WireMock stubs, messages to publish, the client of a user, or NSubstitute setups
// Act       — exactly one action: a method call, an HTTP request or a published message
// Assert    — status code and body, returned value, database state, broker messages, calls received by WireMock
```

- Sections with nothing to do are omitted (a test with no Arrange starts at `// Act`), never merged.
- **Expected exceptions** keep the three sections: the Act captures the call as a delegate
  (`var act = () => sut.RunAsync(...)`), the Assert verifies it (`await Assert.ThrowsAsync<T>(act)`).
- **One behaviour per test**; `[Theory]` for data variations of the same behaviour.
- **Names** `Method_Scenario_ExpectedResult`, unless the repository uses another convention.
- **Folders mirror production code**; one test class per production class or endpoint group.
- Pass `CancellationToken` (the test base property, from `TestContext.Current`) to every async call that accepts one.
- Tests never depend on concrete id values (`/orders/1`): ids come from test repositories or `TestUsers`.

Covering an endpoint ("add tests for endpoint X"): read the endpoint, its contracts, validators, authorization
(attributes, policies, `RequireAuthorization`, fallback policy) and every side effect. Then cover, as applicable:

1. Success: status + body + database state + published message(s) + third-party call(s).
2. Validation failures: one test per rule family, or a `[Theory]` → 400 with the failing member names.
3. Not found / conflict / business rule violations.
4. Authorization: anonymous → 401; a `[Theory]` over `TestUser` values with `CreateClient(user)` and the expected
   status per identity, including `AuthenticatedWithoutRoles` → 403 and every allowed role → success; ownership rules
   with two identities in one test. The per-service authentication matrix is in `local-jwt-authentication.md`.
5. Third-party failure (WireMock 5xx or delay) → the documented behaviour, and nothing persisted/published if that is
   the contract.
6. Absence when a failure must not persist or publish anything (count = 0, no message within a short timeout).

Assert status code first, then the body (`ReadFromJsonAsync<T>(CancellationToken)`; validation errors as
`ValidationProblemDetails`, checking member names, not messages, unless they are the contract), database state through
test repositories (not another endpoint of the same app, unless reading back is the behaviour), messages through the
broker helper, third-party calls through WireMock's request log.

## Identity

- `[AuthenticatedAs(TestUser.X)]` on the method or class sets the identity of `Client`; the method attribute wins.
- `[Unauthenticated]` cancels a class-level `[AuthenticatedAs]` for one test.
- No attribute → anonymous `Client`: who makes each call is always visible in the test.
- `CreateClient(user)` in Arrange when the identity varies per `[Theory]` row (attribute values cannot change per
  `InlineData` row) or a test needs several identities (A creates, B tries to access).
- `CreateClientWithInvalidToken(kind, user)` for authentication tests (expects 401).
- Tokens are always shaped for the application under test (`Fixture.ApplicationResource`); `FunctionalTestBase`
  creates clients for other applications of a flow with that application's audience and claim types.
- Never obtain tokens from an application's login endpoint except in the tests of that endpoint.

## Asynchrony, time and determinism

- Eventual effects: `Eventually.AssertAsync(...)` / `WaitFor…` helpers, polling with a timeout. Never `Thread.Sleep`
  or a fixed `Task.Delay`: too short on a slow agent, wasted time everywhere else.
- Messages: helpers consume with a unique group id and only see messages produced after the test started; if Arrange
  makes the app publish, take a new snapshot (`Fixture.Kafka.CaptureOffsetsAsync()`) right before the Act.
- **Time**: if the app uses `TimeProvider`, replace it with `FakeTimeProvider` in the factory
  (`ReplaceWithSingleton<TimeProvider>`), expose it from the fixture, reset it in `PrepareForTestAsync` with
  `AdjustTime(start)` (`SetUtcNow` cannot move backwards) and advance it with `Advance(...)`. Older package versions
  have no `AdjustTime`: register a small delegating `TimeProvider` whose target is a new `FakeTimeProvider` per test.
  Start the fake clock near the real time, or token lifetimes validated against it turn every request into a 401. If
  the app reads `DateTime.UtcNow` directly, report it as an improvement and write tests that do not depend on the exact
  time.
- Component tests: remove `BackgroundService`s that interfere and are not under test
  (`services.RemoveHostedService<T>()`); never `RemoveAll<IHostedService>()`, which removes the web server.
- Bogus with fixed seeds (`database.md` § Builders and Bogus).

## The fixture: one environment per test project

`TestServerFixtureBase` (Shared) holds everything component and functional tests share:

- starts the shared AppHost through `DistributedApplicationTestingBuilder` in the project's mode, with the dashboard
  disabled and this run's token-trust material as arguments, and waits for the infrastructure;
- waits for the schema initializer (or, when the app migrates itself, for the schema to be complete);
- creates `TestTokenIssuer` (new key per run), the database resetters, test repositories and helpers;
- disposes everything in reverse order: in-process app (component), helpers and data services, then the Aspire app,
  which removes the containers.

The environment starts **once per test project**: an assembly fixture in xUnit v3
(`[assembly: AssemblyFixture(typeof(ComponentTestFixture))]`, injected into each test class constructor) or a
collection fixture in v2. Starting containers per class or test multiplies run time and buys nothing: isolation comes
from resetting data. The fixtures reference the AppHost type through the alias:
`typeof(apphost::Projects.{App}_AppHost)` (`architecture-and-layout.md` § Project references).

## Startup order

1. **AppHost and infrastructure**: build and start with `--RunMode=<mode>` and `TokenIssuer.AppHostArguments()`, then
   `App.ResourceNotifications.WaitForResourceHealthyAsync` for each infrastructure resource. Tests that start before
   the database accepts connections fail at random.
2. **Schema**: `WaitForSuccessfulCompletionAsync(initializer)` (terminal state with exit code 0, otherwise a clear
   error); see `database.md` § Schema in test modes for apps that migrate themselves.
3. **Test-side services and helpers**: a small `ServiceCollection` registering the app's `DbContext` exactly like
   production (prefer the app's own registration extension), the broker helper (topics created here, before any app
   consumer subscribes) and the WireMock client.
4. **Application**: in-process factory (component) or the Aspire project resource started explicitly (functional).
5. **Database resetters**: after the schema exists (Respawn reads the table graph when created).
6. **Test repositories, surviving data and readiness**: `SeedPersistentDataAsync` once (a first test without
   `[ResetDatabase]` needs the test users too), then readiness, e.g. the app answers HTTP, or its Kafka consumer groups
   are Stable with partitions assigned.

Use a generous startup timeout (5 minutes by default): the first run pulls images. One startup hang of `App.StartAsync`
was observed once in a first functional run on 2026-10-23 and did not reproduce in later runs; if it happens, rerun
before investigating and report it.

## Component vs functional fixture

| | Component | Functional |
|---|---|---|
| AppHost mode | `Component` (infrastructure + schema) | `Functional` (infrastructure + schema + apps with `WithExplicitStart`) |
| Application | `ComponentAppFactory : WebApplicationFactory<Program>` in-process | Aspire project resource, separate process, dynamic port |
| Configuration | `UseSetting` + last in-memory source with `AppHostContract` keys, verified after start | `WithReference`/`WithEnvironment` in the AppHost |
| Token trust | `TokenIssuer.TrustSettings(resource)` + `EnsureNoRemoteTokenMetadataAsync` | `TokenIssuer.AppHostArguments()`; `OutboundCallRecorder` as proxy |
| Outbound safety net | `BlockExternalHttpHandler` on every `IHttpClientFactory` client | proxy variables → recorder (refuses and records) |
| Client | `Factory.CreateClient(...)` | `App.CreateHttpClient(resource, "http")` |
| DI access | allowed for scenarios that cannot be provoked otherwise (`CreateClientWithServices`) | none, by design |

Both start the application only after the schema exists and both dispose in reverse order of creation.

## Test base classes

`TestBase<TFixture>` implements `IAsyncLifetime`. In `InitializeAsync` it:

1. gets the running test method from `TestContext.Current` (`(TestContext.Current.TestMethod as IXunitTestMethod)?.Method`,
   namespace `Xunit.v3`; class-level attributes come from `GetType()`);
2. calls `Fixture.PrepareForTestAsync` (shared infrastructure state);
3. reads `[ResetDatabase]` (method, else class), runs the resetters asynchronously, then `SeedPersistentDataAsync`;
4. reads `[Unauthenticated]`/`[AuthenticatedAs]` (method first, then class; none → anonymous) and builds `Client`.

It exposes `Fixture`, `Client`, `CreateClient(TestUser)`, `CreateAnonymousClient()`, `CreateClientWithInvalidToken`,
`CancellationToken`, and disposes every client it created. `ComponentTestBase` adds `CreateClientWithServices(...)`;
`FunctionalTestBase` adds clients for other application resources and nothing else, on purpose.

Why the work is here and not in the attributes: xUnit's `BeforeAfterTestAttribute` hooks are synchronous while resets
are asynchronous, and blocking on async I/O inside the xUnit pipeline is a known deadlock source.

## Per-test reset of shared state

`Fixture.PrepareForTestAsync` runs before every test regardless of attributes and leaves no state from the previous
test in shared infrastructure:

| State | Reset |
|---|---|
| Test data seed | `TestData.BeginTest(testName)` |
| WireMock (third parties) | reset mappings and request log; never reset an `identity` stub |
| Kafka | `CaptureOffsetsAsync()` (again right before the Act when Arrange makes the app publish) |
| App consumers | wait until the app processed everything (`WaitUntilConsumedAsync`) before the database reset |
| RabbitMQ / Service Bus | purge the app's queues/subscriptions if a failed test may have left messages |
| MailPit | `DELETE /api/v1/messages` |
| Fake clock | `fakeTimeProvider.AdjustTime(start)` |
| In-memory state of the in-process app | e.g. `(cache as MemoryCache)?.Clear()`; static caches are a finding to report |
| Redis | a `RedisDatabaseResetter` (it is data, so it belongs to `[ResetDatabase]`) |

The `OutboundCallRecorder` is not reset per test: it must stay empty for the whole run.

## Serial execution and cancellation

- Component and functional projects run their tests one after another: `xunit.runner.json` with
  `"parallelizeTestCollections": false`, copied to the output. All tests share one database per run, so a parallel
  test's reset would delete another test's data.
- The projects themselves can run at the same time (`dotnet test <solution>` does): each starts its own containers on
  random ports. Unit tests keep xUnit's default parallelism.
- Pass `CancellationToken` (from `TestContext.Current`) to every async call in test code; shared helpers and
  repositories use `TestCancellation.Token` internally (xUnit analyzer xUnit1051 flags missing tokens).

## Component tests

One deployable service in-process through `WebApplicationFactory<Program>` against the real infrastructure of the
Component mode, with third-party APIs in WireMock. `ComponentAppFactory` changes only what configuration cannot:

- **Configuration**: the fixture builds a settings dictionary with `AppHostContract` keys (connection strings from
  `App.GetConnectionStringAsync`, endpoints from `App.GetEndpoint`) plus the token trust; the factory applies it with
  `builder.UseSetting(key, value)` (values read eagerly in `Program.cs` before `Build()`, only when `Program.cs` calls
  `WebApplication.CreateBuilder(args)`) and `ConfigureAppConfiguration(c => c.AddInMemoryCollection(settings))` (added
  last, wins for values read later). After start the fixture checks that `IConfiguration` returns every test value and
  fails otherwise: a misspelled key would silently leave a developer database, a real URL or a production `Authority`
  in place. If a value still does not arrive (a configuration source added later, `CreateBuilder()` without args), set
  a process environment variable before the factory starts (acceptable: the project runs serially) and record the
  finding.
- **Environment**: `UseEnvironment(...)` with the name chosen in discovery (the factory default, Development, can turn
  on dev-only behaviour production lacks).
- **Scope validation** (`ValidateScopes`, `ValidateOnBuild`) on in every environment.
- **Outbound guard** `BlockExternalHttpHandler` on every `IHttpClientFactory` client (`external-services.md`).
- **Token trust** through the settings and the metadata guard (`local-jwt-authentication.md` § Trust in component tests).
- `public partial class Program;` only on .NET 8/9 apps (.NET 10 generates it): a declared, minimal production change.
- The fixture forces startup (`_ = Factory.Server`) so startup errors appear in the fixture, not in the first test.

Entry points: HTTP (`Client` or `CreateClient(user)`); message consumers (Act publishes to the real broker exactly as
the real producer, Assert waits with `Eventually`); scheduled jobs (trigger them as production does, or resolve the
job from `Fixture.Factory.Services` in a scope, and wait for the effect).

Replacing services: only when a scenario cannot be provoked through public entry points or WireMock (a specific time,
an exception from an internal dependency, a feature flag provider without configuration). Use
`CreateClientWithServices(...)` (a separate app instance; keep these tests few) or, for a change all tests need,
`ConfigureTestServices` in the factory; replace with the smallest fake (`ReplaceWithSingleton`). The separate instance
runs its own hosted services: remove its message consumers (`RemoveHostedService<T>()`), or a second consumer joins
the app's group, forces a rebalance and makes later tests flaky. Prefer WireMock faults to DI fakes for third-party
failures: they also exercise the HTTP client, serialization and resilience policies.

Applications that are not web apps: a worker whose composition is exposed as an extension
(`services.AddOrdersWorker(configuration)`) gets a host built by the component fixture
(`Host.CreateApplicationBuilder()` + the same settings + that extension); otherwise run it as an Aspire resource in
component mode too (black box, through messages) and say so. Azure Functions (isolated): unit-test the functions and
run the Functions host through Aspire for component/functional coverage, or ask.

## Functional tests

End-to-end flows with the whole system running under the shared AppHost in Functional mode: the application(s) as
project resources, the infrastructure and the third-party substitutes.

- **Black box**: HTTP and messages only. No `WebApplicationFactory`, no access to the app's DI, no replaced services.
  A scenario that needs DI replacement is a component test.
- **Arrange may seed the database** through test repositories; building every precondition through the API would make
  tests long and coupled to unrelated endpoints.
- **Few tests**, each a complete journey that matters; edge cases, validation and role matrices belong to component
  tests. Each application still gets the minimal authentication set out of process
  (`local-jwt-authentication.md` § Authentication and authorization tests).
- **Fixture**: apps are declared with `WithExplicitStart()` and `WaitFor`/`WaitForCompletion` on their dependencies; the
  fixture registers identity stubs (only if strategy C), creates topics, then starts each app with
  `App.ResourceCommands.ExecuteCommandAsync(resource, KnownResourceCommands.StartCommand)` (in 13.6 read
  `result.Message`; `ErrorMessage` is obsolete and breaks builds with warnings as errors) and
  `WaitForResourceHealthyAsync`.
- **Readiness**: with `WithHttpHealthCheck` in that mode, "healthy" means the app answers; without it, it only means
  the process runs: poll a cheap anonymous endpoint (`Eventually.GetAsync`) before the first test.
- **Configuration**: everything through the AppHost (`AppHostContract` keys); the app process cannot be configured any
  other way. The recorder proxy makes any missed real URL fail fast and visible.
- **Several applications**: declare each in the AppHost with its own configuration, trust mapping and waits, list them
  in `ApplicationResources` in start order, and use `CreateClient(user, "billing-api")` /
  `CreateAnonymousClient("billing-api")`. A flow spanning apps asserts the outcome where it becomes visible.
- **Writing a flow test**: Arrange seeds what the flow starts from, stubs third parties, picks the identity; Act starts
  the flow with one HTTP call or one published message; Assert checks the immediate response, then each observable
  outcome (messages after the per-test snapshot, eventual state with `Eventually`, WireMock calls).

## Startup failures and diagnostics

- Container runtime missing or unhealthy: the fixture fails during `StartAsync`; the prerequisites step should have
  caught it, and the tests README troubleshooting table covers it for humans.
- A resource never becomes healthy or the initializer exits non-zero: read its logs (`docker logs`, or Aspire's
  `ResourceLoggerService` from `App.Services`) and include them in the reported error.
- Fixture failures fail every test of the project with the same exception: fix the fixture before looking at tests.
- If `InitializeAsync` throws, xUnit does not call `DisposeAsync`; Aspire cleans up leaked containers when the test
  process exits.

## xUnit v2 variant

Only when the repository already uses xUnit v2 (never introduce it):

- `IAsyncLifetime` uses `Task InitializeAsync()` / `Task DisposeAsync()`.
- No assembly fixtures: define a collection and put every test class in it (do not rely on inheriting `[Collection]`):
  ```csharp
  [CollectionDefinition(Name)]
  public sealed class ComponentCollection : ICollectionFixture<ComponentTestFixture> { public const string Name = "Component"; }

  [Collection(ComponentCollection.Name)]
  public class OrdersEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture) { ... }
  ```
- No `TestContext.Current`, so the base class cannot see the running method. Make `[ResetDatabase]` a
  `BeforeAfterTestAttribute` that resets in `Before`, blocking on the async call (acceptable only because these
  projects run serially), with access to the fixture through a static set by the fixture:
  ```csharp
  public static class TestEnvironment
  {
      // Set by the fixture at the end of InitializeAsync: TestEnvironment.Current = this;
      public static TestServerFixtureBase Current { get; set; } = null!;
  }

  // In ResetDatabaseAttribute : BeforeAfterTestAttribute (Xunit.Sdk namespace in v2)
  private static MethodInfo? _resetDoneFor;

  public override void Before(MethodInfo methodUnderTest)
  {
      // xUnit v2 runs class-level and method-level attributes: reset once, with the effective one (method first).
      if (_resetDoneFor == methodUnderTest) return;
      _resetDoneFor = methodUnderTest;
      var effective = methodUnderTest.GetCustomAttribute<ResetDatabaseAttribute>()
          ?? methodUnderTest.ReflectedType!.GetCustomAttribute<ResetDatabaseAttribute>()!;
      TestEnvironment.Current.Databases.ResetAsync(effective.Databases, effective.ExcludeTables, CancellationToken.None).GetAwaiter().GetResult();
      TestEnvironment.Current.SeedPersistentDataAsync(CancellationToken.None).GetAwaiter().GetResult();
  }

  public override void After(MethodInfo methodUnderTest) => _resetDoneFor = null;
  ```
- Identity: in v2 (2.9.x) the order is constructor → `InitializeAsync` → attribute `Before` → test → `After` →
  `DisposeAsync`, so `[AuthenticatedAs]`/`[Unauthenticated]` become `BeforeAfterTestAttribute`s that record the
  effective user in `Before` (static holder, serial project; method over class, `[Unauthenticated]` wins, none →
  anonymous), and `Client` is created lazily:
  `protected HttpClient Client => _client ??= CurrentTestIdentity.User is { } user ? CreateClient(user) : CreateAnonymousClient();`.
  Before relying on it, write one probe test (403 for one role, 2xx for another); if the order differs in the installed
  version, drop the attributes and use `CreateClient(user)` only.
- `CancellationToken`: `CancellationToken.None` (or a timeout token); `TestCancellation.Token` returns it.

## Common mistakes

| Mistake | Instead |
|---|---|
| A second AppHost for tests | The shared AppHost in Component or Functional mode |
| Resetting the database after each test | Reset before; failed tests keep their data |
| A database or container per test/class | One environment per test project; reset data only |
| SQLite / EF InMemory / in-memory broker as a substitute | Same engine/broker in a container |
| `Thread.Sleep` / `Task.Delay` to wait for effects | `Eventually` and `WaitFor…` helpers with timeouts |
| Component/functional tests running in parallel | `xunit.runner.json` with `parallelizeTestCollections: false` |
| Reading "the last message" of a topic | Offset snapshot / probe bound before the Act + predicate on the test's ids |
| Async work blocked inside xUnit attributes (v3) | Attributes declare; the base class awaits |
| Tokens from a login endpoint, a test auth scheme, or validation turned off | Locally signed tokens through the trust mapping |
| A 200 taken as proof that no identity provider was called | Component metadata guard; functional `OutboundCallRecorder` |
| JWT `PostConfigure` that leaves a remote `ConfigurationManager` (fallback only) | Set `Configuration` and a `StaticConfigurationManager` in `Configure` and `PostConfigure` |
| Hard-coded ids (`/orders/1`) | Ids returned by test repositories |
| Regenerating fixtures or the AppHost in Incremental mode | Reuse; only additive changes |
| A real URL left in configuration | Override every URL; keep the safety nets on |
| Changing production code so a test passes | Report the bug with the failing test |
| Roles as strings in tests | `TestUser` enum values |
| `CS0433` on `Program` | `Aliases="apphost"` on the AppHost reference |
