# Assets: templates

Starting points to copy into the target repository and adapt. They are written for xUnit v3, C# 12+ (test projects
target net8.0 or later), Aspire 13.x and a sample "Orders" app with PostgreSQL, Kafka, a payments API and JWT roles.
None of them compiles as-is, on purpose: placeholders must be replaced and every `// ADAPT:` comment resolved with
facts from discovery.

## Conventions

- `{{Placeholder}}` tokens (table below) make an unadapted template fail to compile instead of compiling silently.
- `// ADAPT:` marks a repository-specific decision. Resolve each one; delete the comment when the code is final.
- Files and members named `Order*`, `Customer*`, `Payment*` are **examples** of the pattern: create one per real
  entity/endpoint/flow and delete the examples.
- Keep the infrastructure files (fixtures, base classes, attributes, resetters, helpers) structurally as they are;
  adapt names, engines and resources. Their design decisions are explained in the references.
- xUnit v2 repositories: see `references/fixtures.md` ("xUnit v2 variant") for the differences.

| Placeholder | Replace with | Example |
|---|---|---|
| `{{AppNamespace}}` | `using` directive(s) for the app types the file uses (entities, DbContext, DTOs, events, `Program`); several namespaces → several usings | `Orders.Api.Domain` |
| `{{SharedNamespace}}` | root namespace of the Shared project | `Orders.Api.Tests.Shared` |
| `{{UnitTestsNamespace}}` | root namespace of the unit test project | `Orders.Api.UnitTests` |
| `{{ComponentTestsNamespace}}` | root namespace of the component test project | `Orders.Api.ComponentTests` |
| `{{FunctionalTestsNamespace}}` | root namespace of the functional test project | `Orders.Api.FunctionalTests` |
| `{{AppHostProjectType}}` | generated type of the test AppHost project reference (in component/functional projects) | `Projects.Orders_Api_Tests_AppHost` |
| `{{AppProjectType}}` | generated type of the app project reference, inside the AppHost | `Projects.Orders_Api` |

Generated `Projects.*` types replace dots and dashes in the project name with underscores.

## Where each file goes

| Template | Destination | Notes |
|---|---|---|
| `apphost/AppHost.cs` | `tests/orchestration/{App}.Tests.AppHost/AppHost.cs` | resources from discovery; functional wiring under the app's config keys |
| `shared/TestHostContract.cs` | `tests/{App}.Tests.Shared/`, linked into the AppHost as source | resource names, mode key, topics, app configuration keys |
| `shared/Attributes/*.cs` | `tests/{App}.Tests.Shared/Attributes/` | usually unchanged |
| `shared/Auth/TestUser.cs`, `TestUsers.cs` | `tests/{App}.Tests.Shared/Auth/` | users/roles/claims from discovery |
| `shared/Auth/TestAuthSettings.cs` | `tests/{App}.Tests.Shared/Auth/` | issuer, audience, role claim type |
| `shared/Auth/TestTokenIssuer.cs` | `tests/{App}.Tests.Shared/Auth/` | usually unchanged |
| `shared/Auth/IdentityProviderStub.cs` | `tests/{App}.Tests.Shared/Auth/` | functional trust; extra discovery paths for Microsoft.Identity.Web |
| `shared/Database/IDatabaseResetter.cs`, `DatabaseResetters.cs`, `DatabaseResetTarget.cs` | `tests/{App}.Tests.Shared/Database/` | unchanged |
| `shared/Database/DatabaseResetConfiguration.cs` | `tests/{App}.Tests.Shared/Database/` | logical names, tables kept on reset, Mongo collections and Redis key prefixes reset |
| `shared/Database/RespawnDatabaseResetter.cs` | `tests/{App}.Tests.Shared/Database/` | SQL Server, PostgreSQL, MySQL, Oracle |
| `shared/Database/MongoDatabaseResetter.cs`, `RedisDatabaseResetter.cs` | `tests/{App}.Tests.Shared/Database/` | only if the app uses them; allowlists in `DatabaseResetConfiguration` |
| `shared/Data/TestData.cs` | `tests/{App}.Tests.Shared/Data/` | unchanged |
| `shared/Data/*Builder.cs`, `*TestRepository.cs` | `tests/{App}.Tests.Shared/Data/` | examples: one per entity |
| `shared/Fixtures/TestServerFixtureBase.cs` | `tests/{App}.Tests.Shared/Fixtures/` | resources, data services, schema, resetters, repositories |
| `shared/Fixtures/TestBase.cs`, `TestCancellation.cs` | `tests/{App}.Tests.Shared/Fixtures/` | usually unchanged |
| `shared/Http/WireMockTestHelper.cs` | `tests/{App}.Tests.Shared/Http/` | only with third-party HTTP APIs |
| `shared/Messaging/KafkaTestHelper.cs` | `tests/{App}.Tests.Shared/Messaging/` | Kafka only; other brokers in references/messaging.md |
| `shared/Waiting/Eventually.cs` | `tests/{App}.Tests.Shared/Waiting/` | unchanged |
| `component/ComponentTestFixture.cs` | `tests/{App}.ComponentTests/` | settings map; registers the assembly fixture |
| `component/ComponentAppFactory.cs` | `tests/{App}.ComponentTests/` | environment, token trust, hosted services to remove |
| `component/ComponentTestBase.cs`, `TestServiceCollectionExtensions.cs`, `BlockExternalHttpHandler.cs` | `tests/{App}.ComponentTests/` | usually unchanged |
| `component/Endpoints/OrdersEndpointTests.cs` | `tests/{App}.ComponentTests/{mirror of production folders}/` | example |
| `functional/FunctionalTestFixture.cs` | `tests/{App}.FunctionalTests/` | app resources; registers the assembly fixture |
| `functional/FunctionalTestBase.cs` | `tests/{App}.FunctionalTests/` | unchanged |
| `functional/Flows/OrderPaymentFlowTests.cs` | `tests/{App}.FunctionalTests/Flows/` | example |
| `unit/OrderPricingServiceTests.cs` | `tests/{App}.UnitTests/{mirror of production folders}/` | example |
| `xunit.runner.json` | `tests/{App}.ComponentTests/` and `tests/{App}.FunctionalTests/` | include with `CopyToOutputDirectory` |
| `tests-README.md` | `tests/README.md` | fill and trim |

Project files (`.csproj`) are in `references/project-setup.md`, not here: stray project files inside a skill folder can
be picked up by repository-wide build scripts.

## Before trusting a template line

Package APIs move. The templates were written against xunit.v3 4.x, Aspire 13.x, Respawn 6.x, WireMock.Net 2.x,
Confluent.Kafka 2.x and Microsoft.IdentityModel 8.x. If the repository pins other versions and something does not
compile, check that package's current API instead of forcing the template — the intent of each member is documented in
its comments and in the references.
