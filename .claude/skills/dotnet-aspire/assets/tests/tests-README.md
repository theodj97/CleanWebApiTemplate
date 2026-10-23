# Tests

<!-- Template for tests/README.md. ADAPT every {{...}} value and delete what does not apply to this repository. -->

| Project | What it tests | Needs Docker/Podman | Runs in parallel |
|---|---|---|---|
| `{{App}}.UnitTests` | One class or a small cohesive group in isolation (domain logic, validators, mappers, handlers). All dependencies are substitutes. No Aspire. | No | Yes |
| `{{App}}.ComponentTests` | The {{App}} service in-process (`WebApplicationFactory`) against real {{InfrastructureList}} containers started by the shared AppHost in `Component` mode; third-party HTTP APIs simulated with WireMock. | Yes | No (shared database) |
| `{{App}}.FunctionalTests` | End-to-end flows with the applications running as separate processes under the shared AppHost in `Functional` mode. Black box: HTTP and messages only. | Yes | No (shared database) |
| `{{App}}.Tests.Shared` | Fixtures, base classes, attributes, test users and token issuer, database resetters, test repositories, builders, helpers. | — | — |

The AppHost is `local/aspire/{{App}}.AppHost` (the one `aspire run` uses). The fixtures start it with
`--RunMode=Component|Functional`: same topology, separate containers, random ports, no dashboard, no telemetry, no
volumes, empty databases (schema only, no local seed).

## Requirements

- .NET SDK {{SdkVersion}} (see `global.json`).
- Docker Desktop, Docker Engine or Podman running, for component and functional tests. Unit tests need neither.
- Nothing else: containers, test users and signing keys are created by the tests. No real service and no identity
  provider is ever called; any outbound HTTP call of an application under test is refused and reported.

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

The first component/functional run pulls container images and can take several minutes. Component and functional
tests run one after another inside each project because they share one database per run; the projects can run at the
same time, and while the local environment is running, because each starts its own containers on random ports.

## Writing tests

- Structure every test as `// Arrange`, `// Act`, `// Assert`, with a single action in Act.
- Name tests `Method_Scenario_ExpectedResult` and mirror the production folders.
- `[ResetDatabase]` (or `[ResetDatabase("OrdersDb")]`) gives a test empty databases; the reset happens before the
  test, so a failed test leaves its data for inspection.
- `[AuthenticatedAs(TestUser.X)]` sets the identity of `Client`; without an attribute `Client` is anonymous. Use
  `CreateClient(TestUser.X)` when the identity changes per `[Theory]` row or a test needs several identities, and
  `CreateClientWithInvalidToken(...)` for 401 cases. Tokens are signed locally per run; never take them from a login
  endpoint.
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
| `'db-init' ended … with exit code 1` | The schema could not be applied: reproduce with `aspire run` and read `aspire logs db-init`. |
| `Blocked outbound HTTP call to ...` / non-empty `OutboundCalls` | A dependency still points to a real URL (or JwtBearer still has an `Authority`). Map it in `AppHostContract` (substitute or `TrustValue.Empty`). |
| `JwtBearer scheme '…' would download metadata` | The trust mapping does not clear `Authority`/`MetadataAddress` for that application. |
| `The application does not read the test value for '<key>'` | The configuration key differs from what the app reads, or `Program.cs` adds a configuration source later. |
| `'<resetter>' refuses to reset '<host>'` | The resetter points at a non-local database. Use the Aspire connection string, or set `DOTNET_TESTING_ALLOW_REMOTE_DATABASE_RESET=1` if Docker really runs on another machine. |
| 401 in every authenticated test | Issuer/audience/claim types in `TestAuthSettings` or the trust mapping do not match what the app validates. |
| 403 where success was expected | Token accepted; the role/scope claim type or value does not match the policy. |
| `CS0433: The type 'Program' exists in both …` | Reference the AppHost with `Aliases="apphost"` and use `extern alias apphost;` in the fixtures. |
