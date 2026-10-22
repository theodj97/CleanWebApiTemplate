# Tests

<!-- Template for tests/README.md. ADAPT every {{...}} value and delete what does not apply to this repository. -->

| Project | What it tests | Needs Docker/Podman | Runs in parallel |
|---|---|---|---|
| `{{App}}.UnitTests` | One class or a small cohesive group in isolation (domain logic, validators, mappers, handlers). All dependencies are substitutes. | No | Yes |
| `{{App}}.ComponentTests` | The {{App}} service as a component: in-process (`WebApplicationFactory`) against real {{InfrastructureList}} containers started by Aspire; third-party HTTP APIs simulated with WireMock. | Yes | No (shared database) |
| `{{App}}.FunctionalTests` | End-to-end business flows with the whole system running under Aspire. Black box: HTTP and messages only. | Yes | No (shared database) |
| `{{App}}.Tests.Shared` | Fixtures, base classes, attributes, test users and token issuer, database resetters, test repositories, builders, helpers. | — | — |
| `orchestration/{{App}}.Tests.AppHost` | Test-only Aspire AppHost. `--TestMode=Component` starts the infrastructure; `--TestMode=Functional` also starts the application(s). No dashboard, no telemetry, no volumes. | — | — |

## Requirements

- .NET SDK {{SdkVersion}} (see `global.json`).
- Docker Desktop, Docker Engine or Podman running, for component and functional tests. Unit tests need neither.
- Nothing else: containers, test users and signing keys are created by the tests. No real service is ever called.

## Running the tests

<!-- ADAPT: keep the variant that matches the repository's test runner (see global.json "test.runner"). -->

```bash
# Everything
dotnet test {{SolutionFile}}

# One type
dotnet test --project tests/{{App}}.UnitTests        # MTP mode (.NET 10 SDK with "test.runner" = Microsoft.Testing.Platform)
dotnet test tests/{{App}}.UnitTests                  # VSTest mode

# One class
dotnet test --project tests/{{App}}.ComponentTests --filter-class "*OrdersEndpointTests"          # MTP
dotnet test tests/{{App}}.ComponentTests --filter "FullyQualifiedName~OrdersEndpointTests"       # VSTest
```

The first component/functional run pulls container images and can take several minutes. Later runs start in seconds.
Component and functional tests run one after another inside each project because they share one database per run;
the two projects can still run at the same time because each starts its own containers on random ports.

## Writing tests

- Structure every test as `// Arrange`, `// Act`, `// Assert`, with a single action in Act.
- Name tests `Method_Scenario_ExpectedResult` and mirror the production folders.
- `[ResetDatabase]` (or `[ResetDatabase("OrdersDb")]`) gives a test empty databases; the reset happens before the
  test, so a failed test leaves its data for inspection.
- `[AuthenticatedAs(TestUser.X)]` sets the identity of `Client`; without an attribute `Client` is anonymous.
  Use `CreateClient(TestUser.X)` when the identity changes per `[Theory]` row or a test needs several identities.
- Insert data with the test repositories (`Fixture.Orders.AddAsync(new OrderBuilder().Build())`) and never rely on
  concrete id values.
- Stub third-party APIs in Arrange (`Fixture.WireMock.StubJsonAsync(...)`) and verify the calls in Assert.
- Wait for eventual effects with `Eventually.AssertAsync(...)` / `Fixture.Kafka.WaitForMessageAsync(...)`; never with
  `Thread.Sleep` or a fixed `Task.Delay`.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Container runtime unhealthy / cannot connect to Docker | Start Docker Desktop or Podman and retry. |
| Podman installed but Aspire looks for Docker | Set `ASPIRE_CONTAINER_RUNTIME=podman` in the environment that runs `dotnet test`. |
| Fixture times out on the first run | Image pull on a slow connection: run again, or raise `StartupTimeout` in the fixture. |
| `Blocked outbound HTTP call to ...` | A dependency still points to a real URL. Map its setting to WireMock in `TestHostContract` and the fixtures. |
| `The application reads '<key>' = ... instead of the test value` | The configuration key differs from what the app reads, or Program.cs adds a configuration source later. |
| `'<resetter>' refuses to reset '<host>'` | The resetter points at a non-local database. Use the Aspire container connection string, or set `DOTNET_TESTING_ALLOW_REMOTE_DATABASE_RESET=1` if Docker really runs on another machine. |
| 401 in every authenticated test | Issuer/audience/role claim in `TestAuthSettings` do not match what the app validates. |
