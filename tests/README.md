# Tests

| Project | What it tests | Needs Docker | Runs in parallel |
|---|---|---|---|
| `CleanWebApiTemplate.Api.UnitTests` | Validators and other pure logic in isolation. Dependencies are substitutes; no Aspire, no containers. | No | Yes |
| `CleanWebApiTemplate.OpenApiExamples.UnitTests` | The local OpenAPI examples merger: path normalization, body and parameter examples, catalog errors. No Aspire, no containers. | No | Yes |
| `CleanWebApiTemplate.Api.ComponentTests` | The API in-process (`WebApplicationFactory<Program>`) against the real PostgreSQL and OpenBao containers started by the shared AppHost in `Component` mode. Covers endpoints, validation, database state and the authentication/authorization matrix. | Yes | No (shared database) |
| `CleanWebApiTemplate.Api.FunctionalTests` | Complete flows with the API running as a separate process under the shared AppHost in `Functional` mode. Black box: HTTP only. | Yes | No (shared database) |
| `CleanWebApiTemplate.Tests.Shared` | Fixtures, base classes, attributes, test users and token issuer, database resetters, test repository, builders and helpers. | — | — |

The AppHost is `local/aspire/CleanWebApiTemplate.AppHost` (the one `aspire run` uses). The fixtures start it with
`--RunMode=Component|Functional`: same topology, separate containers, random ports, no dashboard, no telemetry, no
volumes, empty databases (schema only, no local seed) and a per-run OpenBao root token passed as an argument.

## Requirements

- .NET SDK 10.0.113 (see `global.json`; it also selects the Microsoft Testing Platform runner).
- Docker Engine/Desktop running, for component and functional tests. Unit tests need neither.
- Nothing else: containers, test users and signing keys are created by the tests. The signing key is seeded into
  OpenBao by the AppHost and read by the application from there. No real service and no identity provider is ever
  called; any outbound HTTP call of the application under test is refused and reported.

## Running the tests

`global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`, so `dotnet test` uses MTP and requires
`--project` or `--solution`:

```bash
# Everything (unit, component and functional)
dotnet test --solution CleanWebApiTemplate.slnx

# One project
dotnet test --project tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj
```

The first component/functional run pulls the PostgreSQL and OpenBao images and can take a few minutes. Component and
functional tests run one after another inside each project because they share one database per run; the projects can
run at the same time, and while the local environment is running, because each starts its own containers on random
ports.

## Writing tests

- Structure every test as `// Arrange`, `// Act`, `// Assert`, with a single action in Act.
- Name tests `Method_Scenario_ExpectedResult` and mirror the production folders.
- `[ResetDatabase]` (or `[ResetDatabase("TodoDb")]`) gives a test an empty `Todo` table; the reset happens before the
  test, so a failed test leaves its data for inspection.
- `[AuthenticatedAs(TestUser.X)]` sets the identity of `Client`; without an attribute `Client` is anonymous. Use
  `CreateClient(TestUser.X)` when the identity changes per `[Theory]` row or a test needs several identities, and
  `CreateClientWithInvalidToken(...)` for 401 cases. Tokens are signed locally per run; never take them from a login
  endpoint.
- Insert data with the test repository (`Fixture.Todos.AddAsync(new TodoBuilder().Build())`) and never rely on
  concrete id values.
- Wait for eventual effects with `Eventually.AssertAsync(...)`; never with `Thread.Sleep` or a fixed `Task.Delay`.

## Test identities

The API policies are role based (`Admin`, `Operator`, `User`, `External`). `TestUser` covers one identity per role
plus an authenticated identity without roles:

| `TestUser` | Token role | Todo endpoints |
|---|---|---|
| `TodoUser` | `User` | read, create, update; delete → 403 |
| `TodoOperator` | `Operator` | read, create, update, delete |
| `TodoAdmin` | `Admin` | everything |
| `TodoExternal` | `External` | 403 on the Todo endpoints (no endpoint uses the External policy) |
| `AuthenticatedWithoutRoles` | — | 403 on every protected endpoint |

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Specifying a project for 'dotnet test' should be via '--project'.` | The MTP runner is active: use `--project`/`--solution`. |
| Container runtime unhealthy / cannot connect to Docker | Start Docker Desktop or Podman and retry. |
| Podman installed but Aspire looks for Docker | Set `ASPIRE_CONTAINER_RUNTIME=podman` in the environment that runs `dotnet test`. |
| Fixture times out on the first run | Image pull on a slow connection: run again, or raise `StartupTimeout` in `TestServerFixtureBase`. |
| `'db-init' ended … with exit code 1` | The schema could not be applied: reproduce with `aspire run` and read `aspire logs db-init`. |
| `'openbao-init' ended … with exit code 1` | The secrets could not be written: reproduce with `aspire run` and read `aspire logs openbao-init`. |
| `'API_KEY' is required` | The application read OpenBao but the secret was not there: check that `openbao-init` finished with exit code 0 and that `OpenBao:Path` matches. |
| `The application did not read 'API_KEY' from OpenBao` | A stray `API_KEY` in the test process, or the initializer did not seed the store. |
| `Blocked outbound HTTP call to ...` / non-empty `OutboundCalls` | A dependency still points to a real URL (or JwtBearer still has an `Authority`). Map it in `AppHostContract` (substitute or `TrustValue.Empty`). |
| `JwtBearer scheme '…' would download metadata` | The trust mapping does not clear `Authority`/`MetadataAddress` for that application. |
| `The application does not read the test value for '<key>'` | The configuration key differs from what the app reads, or `Program.cs` adds a configuration source later. |
| 401 in every authenticated test | Issuer/audience/claim types in `TestAuthSettings` or the trust mapping do not match what the app validates. |
| 403 where success was expected | Token accepted; the role/claim type or value does not match the policy. |
| `CS0433: The type 'Program' exists in both …` | Reference the AppHost with `Aliases="apphost"` and use `extern alias apphost;` in the fixtures. |
