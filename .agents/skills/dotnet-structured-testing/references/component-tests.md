# Component tests

Templates: `assets/component/*` (fixture, factory, base class, DI helpers, outbound guard, example tests).

## Contents
- [Scope](#scope)
- [The factory](#the-factory)
- [Entry points](#entry-points)
- [What to assert](#what-to-assert)
- [Covering an endpoint](#covering-an-endpoint)
- [Replacing services](#replacing-services)
- [Time](#time)
- [Background services](#background-services)
- [Applications that are not web apps](#applications-that-are-not-web-apps)
- [Example](#example)

## Scope

One deployable service treated as a component: it runs in-process through `WebApplicationFactory<Program>` against the
real infrastructure Aspire starts (database, broker, cache), with third-party APIs simulated by WireMock. Tests enter
through the service's public entry points (HTTP endpoints, message consumers) and verify the response, the database
state, the published messages and the calls to third parties. Most integration tests belong here: they are fast
(in-process, shared environment) and can still provoke rare scenarios.

## The factory

`ComponentAppFactory` (template) changes only what configuration cannot:

- **Configuration**: the fixture builds a settings dictionary with `TestHostContract.AppSettings` keys and values from
  Aspire (`App.GetConnectionStringAsync`, `App.GetEndpoint`). The factory applies it twice, because .NET reads
  configuration at two different moments:
  - `builder.UseSetting(key, value)` reaches values read eagerly in `Program.cs` before `Build()` — but only when
    `Program.cs` calls `WebApplication.CreateBuilder(args)`; with `CreateBuilder()` (no args) the values never arrive;
  - `ConfigureAppConfiguration(c => c.AddInMemoryCollection(settings))` is added last, so it wins for values read
    later (`IOptions<T>`, `IConfiguration` at runtime).
  After the app starts, the fixture checks that `IConfiguration` returns each test value and fails otherwise: a
  misspelled key would silently leave a developer database or a real URL in place. If a value still does not arrive
  (Program.cs adds a configuration source after the defaults, or uses `CreateBuilder()` without args), set it as a
  process environment variable before the factory starts — acceptable here because the project runs serially — and
  record the finding.
- **Environment**: `UseEnvironment(...)` with the name chosen in discovery. The factory's default is Development, which
  can switch on behaviour production does not have (dev seeding, exception pages, Swagger).
- **Scope validation** (`ValidateScopes`, `ValidateOnBuild`) stays on in every environment, so DI lifetime mistakes
  surface in tests.
- **Token trust** for every JwtBearer scheme (auth.md).
- **Outbound guard** `BlockExternalHttpHandler` on every `IHttpClientFactory` client (external-services.md).
- `public partial class Program;` only on .NET 8/9 apps (.NET 10 generates it): a declared, minimal production change.

The fixture forces the app to start once (`_ = Factory.Server`) so startup errors appear in the fixture, not in the
first test.

## Entry points

- **HTTP**: `Client` (identity from attributes) or `CreateClient(user)`. Pass `CancellationToken` to every call.
- **Message consumers**: Act publishes to the real broker exactly as the real producer does
  (`Fixture.Kafka.PublishAsync(...)`); Assert waits for the effect with `Eventually.AssertAsync` (messaging.md).
- **Scheduled jobs / background processing**: trigger them through the mechanism production uses (a message, an
  endpoint, the job's public method resolved from `Fixture.Factory.Services` in a scope) and wait for the effect.

## What to assert

- Status code first, then the body: `ReadFromJsonAsync<T>(CancellationToken)`; validation errors as
  `ValidationProblemDetails` (check the failing member names, not the exact messages unless they are the contract).
- Database state through the test repositories (`Fixture.Orders.GetSingleByCustomerAsync(...)`), not through another
  endpoint of the same app — unless reading back is the behaviour being verified.
- Published messages through the broker helper (`Fixture.Kafka.WaitForMessageAsync<T>(...)`).
- Third-party calls through WireMock's request log (`Fixture.WireMock.GetRequestsAsync(...)`).
- Absence: when a failure must not persist or publish anything, assert it (count = 0, no message within a short timeout).

## Covering an endpoint

For "add tests for endpoint X", read the endpoint, its request/response contracts, validators, authorization
(attributes, policies, `RequireAuthorization`, fallback policy), and every side effect (database writes, messages,
third-party calls). Then cover, as applicable:

1. Success: status + body + database state + published message(s) + third-party call(s).
2. Validation failures: one test per rule family, or a `[Theory]` over invalid inputs → 400 with the member names.
3. Not found / conflict / business rule violations.
4. Authorization:
   - anonymous → 401 (no attribute, or `[Unauthenticated]` in an authenticated class);
   - a `[Theory]` over `TestUser` values with `CreateClient(user)` and the expected status per role, including
     `AuthenticatedWithoutRoles` → 403 and every allowed role → success;
   - ownership rules: two identities in one test (A creates, B reads → 403/404).
5. Third-party failure (WireMock 5xx or delay) → the app's documented behaviour, and nothing persisted/published if
   that is the contract.

## Replacing services

Allowed only when a scenario cannot be provoked through public entry points or WireMock: a specific time, an exception
from an internal dependency, a feature flag provider without configuration. Use `CreateClientWithServices(...)` from
`ComponentTestBase` (a separate app instance built with `WithWebHostBuilder` — keep these tests few) or, for a change all
tests need, `ConfigureTestServices` in the factory. Replace with the smallest fake possible (`ReplaceWithSingleton`).
The separate instance runs its own hosted services: remove its message consumers in the same callback
(`RemoveHostedService<T>()`), otherwise a second consumer joins the app's consumer group, forces a rebalance, and the
main app's consumers need time to recover — making the following tests flaky.

Prefer WireMock faults to DI fakes for third-party failures: they also exercise the HTTP client, serialization and
resilience policies.

## Time

If the app uses `TimeProvider`, replace it with `FakeTimeProvider` in the factory (`ReplaceWithSingleton<TimeProvider>`)
and expose it from the fixture. Reset it in `PrepareForTestAsync` with `AdjustTime(start)` — `SetUtcNow` throws when
moving backwards — and advance it in tests with `Advance(...)`. Older versions of the package have no `AdjustTime`:
register a small delegating `TimeProvider` whose target is a new `FakeTimeProvider` per test instead.

Start the fake clock at (or near) the real current time, or make `TestTokenIssuer` take its times from the same
clock: if authentication validates token lifetimes against the app's `TimeProvider`, a fake clock far from the real
time turns every request into a 401.

If the app reads `DateTime.UtcNow` directly, do not change production code: report it as an improvement and write the
tests so they do not depend on the exact time.

## Background services

Hosted services start with the app. Remove (`services.RemoveHostedService<T>()`) those that interfere with tests and
are not under test — pollers that change data, outbox publishers in tests that do not verify publication, jobs calling
third parties on a timer. Never `RemoveAll<IHostedService>()`: it removes the service that starts the web server.
Keep consumers that the tests exercise, and wait for them to be ready in the fixture.

## Applications that are not web apps

`WebApplicationFactory` hosts web apps only.
- Worker: if the app's composition is exposed as an extension (`services.AddOrdersWorker(configuration)`), the
  component fixture builds its own host with `Host.CreateApplicationBuilder()`, applies the same settings and calls
  that extension; otherwise run the worker as an Aspire project resource in component mode too (black box, entering
  through messages) and say so in the report.
- Azure Functions (isolated): unit-test the function classes; run the Functions host through Aspire for
  component/functional coverage, or ask.

## Example

`assets/component/Endpoints/OrdersEndpointTests.cs`: success with database and message checks, a role `[Theory]`,
anonymous 401, validation 400 and a third-party failure.
