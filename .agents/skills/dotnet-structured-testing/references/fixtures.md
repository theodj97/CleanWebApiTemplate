# Fixtures, base classes, lifecycle and serial execution

Templates: `assets/shared/Fixtures/TestServerFixtureBase.cs`, `assets/shared/Fixtures/TestBase.cs`,
`assets/component/ComponentTestFixture.cs`, `assets/component/ComponentTestBase.cs`,
`assets/functional/FunctionalTestFixture.cs`, `assets/functional/FunctionalTestBase.cs`.
Unit tests have no fixture unless something concrete justifies one.

## Contents
- [The fixture: one environment per test project](#the-fixture-one-environment-per-test-project)
- [Startup order](#startup-order)
- [Component vs functional fixture](#component-vs-functional-fixture)
- [Test base classes](#test-base-classes)
- [Per-test reset of shared state](#per-test-reset-of-shared-state)
- [Serial execution](#serial-execution)
- [Cancellation](#cancellation)
- [Startup failures and diagnostics](#startup-failures-and-diagnostics)
- [xUnit v2 variant](#xunit-v2-variant)

## The fixture: one environment per test project

`TestServerFixtureBase` (Shared) holds everything component and functional tests share:

- starts Aspire through `DistributedApplicationTestingBuilder` (dashboard disabled) and waits until every resource
  is healthy;
- prepares the schema and creates the database resetters;
- creates `TestTokenIssuer` (trust is configured by the derived fixture);
- exposes test repositories and helpers (`Kafka`, `WireMock`, `Eventually` is static); the component fixture also
  exposes the `WebApplicationFactory`;
- disposes everything in `DisposeAsync`, in reverse order of creation: the in-process app (component fixture), then
  the helpers and data services, then the Aspire app, which removes the containers.

The environment starts **once per test project**: an assembly fixture in xUnit v3 —
`[assembly: AssemblyFixture(typeof(ComponentTestFixture))]`, injected into each test class constructor — or a
collection fixture shared by every class in v2. Starting containers per class or per test would multiply the run
time by orders of magnitude and buys nothing, because isolation comes from resetting data, not from new containers.

## Startup order

1. **Infrastructure**: build and start the AppHost, then `App.ResourceNotifications.WaitForResourceHealthyAsync` for
   each infrastructure resource. Tests that start before the database accepts connections fail at random.
2. **Test-side services and helpers**: a small `ServiceCollection` registering the app's DbContext exactly like
   production (prefer calling the app's own registration extension), the Kafka helper (topics created here, before any
   app consumer subscribes) and the WireMock client.
3. **Schema**, applied with the production mechanism before the app starts, so background services never see missing
   tables. Skip it when the app applies the schema itself on startup (`SchemaAppliedByApplication => true`).
4. **Application**: in-process factory (component) or Aspire project resource started explicitly (functional).
5. **Database resetters**: created after the schema exists, because Respawn reads the table graph when created. When
   the app applies the schema itself, first wait until it is complete (no pending migrations).
6. **Test repositories, surviving data and readiness**: `SeedPersistentDataAsync` once (a first test without
   `[ResetDatabase]` needs the test users too), then e.g. wait until the app's Kafka consumer groups are Stable with
   partitions assigned; otherwise the first test's message can be skipped.

Use a generous startup timeout (5 minutes by default): the first run pulls images.

## Component vs functional fixture

| | Component | Functional |
|---|---|---|
| AppHost mode | `Component` (infrastructure only) | `Functional` (infrastructure + apps with `WithExplicitStart`) |
| Application | `ComponentAppFactory : WebApplicationFactory<Program>` in-process | Aspire project resource, separate process |
| Configuration | `UseSetting` + in-memory source with `TestHostContract.AppSettings` keys, verified after start | `WithReference`/`WithEnvironment` in the AppHost |
| Token trust | `JwtBearerOptions` configured in-process (auth.md) | WireMock `identity` resource serves discovery + JWKS (auth.md) |
| Client | `Factory.CreateClient(...)` | `App.CreateHttpClient(resource, "http")` |
| DI access | allowed for scenarios that cannot be provoked otherwise (`CreateClientWithServices`) | none, by design |

Both start the application only after steps 1–3, and both must be disposed in reverse order of creation.

## Test base classes

`TestBase<TFixture>` implements `IAsyncLifetime`. In `InitializeAsync` it:

1. Gets the running test method from `TestContext.Current` —
   `(TestContext.Current.TestMethod as IXunitTestMethod)?.Method` (namespace `Xunit.v3`). xUnit v3 populates the
   context before the test class constructor and `InitializeAsync` run. Verify the cast against the installed
   version; the class-level attributes are read from `GetType()`, the concrete test class.
2. Calls `Fixture.PrepareForTestAsync` (shared infrastructure state, see below).
3. Reads `[ResetDatabase]` from the method, else from the class, and runs the matching resetters asynchronously; then
   `Fixture.SeedPersistentDataAsync` re-inserts data that must survive (e.g. test users).
4. Reads `[Unauthenticated]`/`[AuthenticatedAs]` (method first, then class; none → anonymous) and builds `Client`.

It exposes `Fixture`, `Client`, `CreateClient(TestUser)`, `CreateAnonymousClient()` and `CancellationToken`, and
disposes every client it created. `ComponentTestBase` adds `CreateClientWithServices(...)` for DI replacements;
`FunctionalTestBase` adds nothing on purpose.

Why the work is here and not in the attributes: xUnit's `BeforeAfterTestAttribute` hooks are synchronous
(`Before(MethodInfo, IXunitTest)` in v3) while resets are asynchronous, and blocking on async I/O inside the xUnit
pipeline is a well-known deadlock source. The attributes only declare intent.

## Per-test reset of shared state

`Fixture.PrepareForTestAsync` runs before every test regardless of attributes. It must leave no state from the previous
test in shared infrastructure. Include what applies:

| State | Reset |
|---|---|
| Test data seed | `TestData.BeginTest(testName)` — deterministic data per test, independent of execution order |
| WireMock (third parties) | reset mappings and request log; never reset the `identity` WireMock |
| Kafka | `CaptureOffsetsAsync()` so verification only sees this test's messages (a test whose Arrange makes the app publish calls it again right before the Act) |
| App consumers | if the app consumes messages, wait until its consumer groups have processed everything (`WaitUntilConsumedAsync`) before the database reset, so a late message cannot write into this test's data |
| RabbitMQ / Service Bus | purge the app's queues/subscriptions if a failed test may have left messages |
| MailPit | `DELETE /api/v1/messages` |
| Fake clock | `fakeTimeProvider.AdjustTime(start)` (`SetUtcNow` cannot move backwards) |
| In-memory state of the in-process app | e.g. `IMemoryCache` → `(cache as MemoryCache)?.Clear()`; static caches of the app are a finding to report |
| Redis | a `RedisDatabaseResetter` for the app's key prefixes (it is data, so it belongs to `[ResetDatabase]`) |

## Serial execution

Component and functional projects run their tests one after another: copy `assets/xunit.runner.json`
(`"parallelizeTestCollections": false`) and include it with `CopyToOutputDirectory`. All tests share one database per
run, so a parallel test's reset would delete another test's data mid-flight.

The two projects can still run at the same time: each starts its own containers on random ports through its own
fixture. Unit tests share nothing and keep xUnit's default parallelism.

## Cancellation

Pass `CancellationToken` (from `TestContext.Current`) to every async call in test code that accepts one — HttpClient,
`ReadFromJsonAsync`... — so an aborted run or a timed-out test stops quickly (xUnit analyzer xUnit1051 flags missing
tokens). Shared helpers and repositories take the same token internally (`TestCancellation.Token`), which keeps test
code short.

## Startup failures and diagnostics

- Container runtime missing or unhealthy: the fixture fails during `StartAsync`. The skill's prerequisites step should
  have caught it; the README's troubleshooting table covers it for humans.
- A resource never becomes healthy: read its logs (container logs via `docker logs`, or Aspire's
  `ResourceLoggerService` from `App.Services`) and include them in the error you report.
- Fixture failures fail every test of the project with the same exception: fix the fixture before looking at tests.
- If `InitializeAsync` throws, xUnit does not call `DisposeAsync`; leaked containers are cleaned up by Aspire when the
  test process exits.

## xUnit v2 variant

Only when the repository already uses xUnit v2 (never introduce it). Differences:

- `IAsyncLifetime` uses `Task InitializeAsync()` / `Task DisposeAsync()`.
- No assembly fixtures: define a collection and put every test class in it (do not rely on inheriting `[Collection]`
  from a base class):
  ```csharp
  [CollectionDefinition(Name)]
  public sealed class ComponentCollection : ICollectionFixture<ComponentTestFixture> { public const string Name = "Component"; }

  [Collection(ComponentCollection.Name)]
  public class OrdersEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture) { ... }
  ```
- No `TestContext.Current`, so the base class cannot see the running method. Make `[ResetDatabase]` a
  `BeforeAfterTestAttribute` and reset in its `Before`, blocking on the async call. That is acceptable only because
  these projects run serially; give the attribute access to the fixture through a static set by the fixture:
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
  `DisposeAsync`. So `InitializeAsync` cannot read the identity; instead `[AuthenticatedAs]` (and `[Unauthenticated]`)
  become `BeforeAfterTestAttribute`s that record the effective user in `Before`, and `Client` is created lazily:
  ```csharp
  // [AuthenticatedAs] / [Unauthenticated]: CurrentTestIdentity is a static holder (serial project); Resolve applies the
  // same precedence as TestBase.ResolveUser (method over class, [Unauthenticated] wins, none → anonymous).
  public override void Before(MethodInfo method) => CurrentTestIdentity.User = IdentityResolver.Resolve(method);

  // TestBase (v2)
  private HttpClient? _client;
  protected HttpClient Client => _client ??= CurrentTestIdentity.User is { } user ? CreateClient(user) : CreateAnonymousClient();
  ```
  Before relying on it, write one probe test that asserts the identity reaches the app (an endpoint returning 403 for
  one role and 2xx for another). If the order is not what you expect in the installed version, drop the attributes and
  use `CreateClient(user)` only.
- `CancellationToken`: `CancellationToken.None` (or a timeout token) — `TestCancellation.Token` returns it.
