# Functional tests

Templates: `assets/functional/*`, `assets/apphost/AppHost.cs` (functional mode).

## Scope

End-to-end business flows with the whole system running under Aspire: the application (or several) as project
resources, plus the infrastructure and the third-party substitutes. Example: create an order → an event is published →
a consumer changes its status.

- **Black box**: interact only through HTTP and messages. No `WebApplicationFactory`, no access to the application's
  DI container, no replaced services. If a scenario needs a DI replacement, it is a component test.
- **Arrange may seed the database directly** through the test repositories: building every precondition through the
  API would make each test long and couple it to unrelated endpoints.
- **Few tests**, each a complete journey that matters to the business. Edge cases, validation and per-role matrices
  belong to component tests; repeating them here only adds run time.

## Fixture specifics

`FunctionalTestFixture` (template), on top of the shared startup (fixtures.md):
- AppHost mode `Functional`; the apps are declared with `WithExplicitStart()` and every dependency `WaitFor`.
- Before starting the apps: identity stubs on the `identity` WireMock (`IdentityProviderStub.RegisterAsync`), schema
  (if the app does not apply it), topics. Then `App.ResourceCommands.ExecuteCommandAsync(resource,
  KnownResourceCommands.StartCommand)` and `WaitForResourceHealthyAsync`.
- Readiness: with `WithHttpHealthCheck("/health")` in the AppHost, "healthy" means the app answers; without a health
  endpoint it only means the process runs — then poll a cheap anonymous endpoint with `Eventually.GetAsync` before
  the first test, or the first test races the app's startup.
- Clients: `App.CreateHttpClient(resource, "http")`; `TestBase` adds the bearer token from `TestTokenIssuer`, which the
  app trusts through the identity stub (auth.md).
- Configuration: everything through the AppHost (`TestHostContract.AppSettings` keys) — the app process cannot be
  configured any other way. The proxy variables make any missed real URL fail fast.

## Writing a flow test

1. Arrange: seed what the flow starts from (repositories + builders), stub third parties (`Fixture.WireMock`), pick
   the identity (`[AuthenticatedAs]` or `CreateClient(user)`).
2. Act: one action that starts the flow — usually one HTTP call, or one message published to a topic the system consumes.
3. Assert: the immediate response, then each observable outcome of the flow:
   - messages: `Fixture.Kafka.WaitForMessageAsync<T>(topic, predicate)` (only messages after the per-test snapshot);
   - eventual state: `Eventually.AssertAsync(...)` reading through the public API (black box) or a test repository;
   - third-party calls: `Fixture.WireMock.GetRequestsAsync(...)`.
   Never wait with `Thread.Sleep`/`Task.Delay`.

Example: `assets/functional/Flows/OrderPaymentFlowTests.cs` (seed order → stub provider → pay → event published →
status becomes "Paid" → provider call verified).

## Several applications

Declare each app in the AppHost (functional mode) with its own configuration and `WaitFor`, add it to
`ApplicationResources` in start order, and use `CreateClient(user, "billing-api")` /
`CreateAnonymousClient("billing-api")` from `FunctionalTestBase` for the other apps. A flow spanning apps asserts the
outcome in the app where it becomes visible.

## Diagnostics

When a functional test fails without an obvious reason, read the app's logs (Aspire `ResourceLoggerService` from
`App.Services`, or the console output of the test run) before changing the test. A failure that only reproduces here,
not in component tests, usually points to configuration (a key not injected, the identity stub, a missing topic).
