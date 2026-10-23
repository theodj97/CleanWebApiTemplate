# Assets: templates

Starting points to copy into the target repository and adapt. They are written for .NET 10, C# 12+, xUnit v3 (VSTest
`mtp-off` flavour), Aspire 13.x and an example "Orders" application (PostgreSQL + EF Core, HS256 key in `Jwt:Key`,
its own login endpoint) next to a "Catalog" application (config-bound JwtBearer with a production `Authority`).

Files marked *validated* were instantiated in a validation repository on 2026-10-23 and built and passed (see
`references/sources.md`); the others come from the former skills and were not re-run in this validation.

## Conventions

- `{{Placeholder}}` tokens (table below) make an unadapted template fail to compile instead of compiling silently.
- `// ADAPT:` marks a repository-specific decision. Resolve each one; delete the comment when the code is final.
  Commented `ADAPT` blocks are optional patterns (Kafka, third-party HTTP APIs, identity stub): uncomment only what
  the inventory justifies.
- Files and members named `Order*`, `User*`, `Catalog*`, `Payment*` are **examples** of the pattern: create one per
  real entity, endpoint or flow and delete the examples.
- Keep the infrastructure files (fixtures, base classes, attributes, resetters, helpers, devtoken) structurally as
  they are; adapt names, engines, resources and mappings. Their design decisions are explained in the references.
- `devtoken.cs` is generic: copy it unchanged; only `devtoken.json` is adapted.
- xUnit v2 repositories: `references/integration-testing.md` § xUnit v2 variant.

| Placeholder | Replace with | Example |
|---|---|---|
| `{{AppHostNamespace}}` | namespace of `AppHostContract` (the AppHost project's root namespace) | `Contoso.Orders.AppHost` |
| `{{DbInitializerProjectType}}`, `{{OrdersApiProjectType}}`, `{{CatalogApiProjectType}}` | generated `Projects.*` types inside the AppHost | `Projects.Contoso_Orders_Api` |
| `{{AppHostProjectType}}` | generated type of the AppHost, used through the alias: `apphost::{{AppHostProjectType}}` | `Projects.Contoso_Orders_AppHost` |
| `{{AppNamespace}}` | `using` directive(s) for the app types the file uses (entities, `DbContext`, DTOs, events) | `Contoso.Orders.Data` |
| `{{SharedNamespace}}` | root namespace of the Shared project | `Contoso.Orders.Tests.Shared` |
| `{{UnitTestsNamespace}}` | root namespace of the unit test project | `Contoso.Orders.Api.UnitTests` |
| `{{ComponentTestsNamespace}}` | root namespace of the component test project | `Contoso.Orders.Api.ComponentTests` |
| `{{FunctionalTestsNamespace}}` | root namespace of the functional test project | `Contoso.Orders.Api.FunctionalTests` |

Generated `Projects.*` types replace dots and dashes in the project name with underscores.

## Where each file goes

| Template | Destination | Notes |
|---|---|---|
| `local/apphost/AppHost.cs` | `local/aspire/{App}.AppHost/AppHost.cs` | *validated*; modes, resources, `WithLocalJwtTrust` |
| `local/apphost/AppHostContract.cs` | `local/aspire/{App}.AppHost/`, linked into Shared | *validated*; mode key, resource names, keys, JWT trust mapping |
| `local/tools/devtoken.cs` | `local/tools/devtoken.cs` | *validated*; unchanged |
| `local/tools/devtoken.json` | `local/tools/devtoken.json` | *validated*; issuer, algorithm, audiences, claim names, profiles from discovery |
| `local/local-development.md` | `local/local-development.md` | fill and trim |
| DbInitializer | `local/aspire/{App}.DbInitializer/` | code in `references/database.md` (*validated*) |
| csproj files | each project | in `references/architecture-and-layout.md` § Project files, not here: stray `.csproj` files inside a skill folder can be picked up by repository-wide build scripts |
| `tests/shared/Attributes/*.cs` | `tests/{App}.Tests.Shared/Attributes/` | *validated*; usually unchanged |
| `tests/shared/Auth/TestUser.cs`, `TestUsers.cs`, `TestAuthSettings.cs` | `tests/{App}.Tests.Shared/Auth/` | *validated*; users, roles, scopes, issuer, audiences, claim types from discovery |
| `tests/shared/Auth/TestTokenIssuer.cs` | `tests/{App}.Tests.Shared/Auth/` | *validated*; usually unchanged |
| `tests/shared/Auth/IdentityProviderStub.cs` | `tests/{App}.Tests.Shared/Auth/` | only for strategy C (last resort) |
| `tests/shared/Database/IDatabaseResetter.cs`, `DatabaseResetters.cs`, `RespawnDatabaseResetter.cs` | `tests/{App}.Tests.Shared/Database/` | *validated* (Respawn 7.0.0); unchanged |
| `tests/shared/Database/DatabaseResetConfiguration.cs` | `tests/{App}.Tests.Shared/Database/` | *validated*; tables kept, Mongo collections and Redis prefixes reset |
| `tests/shared/Database/DatabaseResetTarget.cs`, `MongoDatabaseResetter.cs`, `RedisDatabaseResetter.cs` | `tests/{App}.Tests.Shared/Database/` | only if the app uses MongoDB/Redis |
| `tests/shared/Data/TestData.cs` | `tests/{App}.Tests.Shared/Data/` | *validated*; unchanged |
| `tests/shared/Data/*Builder.cs`, `*TestRepository.cs` | `tests/{App}.Tests.Shared/Data/` | *validated* examples: one per entity |
| `tests/shared/Fixtures/TestServerFixtureBase.cs` | `tests/{App}.Tests.Shared/Fixtures/` | *validated*; resources, data services, resetters, repositories |
| `tests/shared/Fixtures/TestBase.cs`, `TestCancellation.cs` | `tests/{App}.Tests.Shared/Fixtures/` | *validated*; usually unchanged |
| `tests/shared/Http/OutboundCallRecorder.cs` | `tests/{App}.Tests.Shared/Http/` | *validated*; unchanged |
| `tests/shared/Http/WireMockTestHelper.cs` | `tests/{App}.Tests.Shared/Http/` | only with third-party HTTP APIs |
| `tests/shared/Messaging/KafkaTestHelper.cs` | `tests/{App}.Tests.Shared/Messaging/` | Kafka only; other brokers in `references/messaging.md` |
| `tests/shared/Waiting/Eventually.cs` | `tests/{App}.Tests.Shared/Waiting/` | *validated*; unchanged |
| `tests/component/ComponentTestFixture.cs`, `ComponentAppFactory.cs` | `tests/{App}.ComponentTests/` | *validated*; settings, environment, hosted services to remove |
| `tests/component/ComponentTestBase.cs`, `TestServiceCollectionExtensions.cs`, `BlockExternalHttpHandler.cs` | `tests/{App}.ComponentTests/` | *validated*; usually unchanged |
| `tests/component/AuthenticationTests.cs`, `Endpoints/OrdersEndpointTests.cs`, `Endpoints/LoginEndpointTests.cs` | `tests/{App}.ComponentTests/{mirror of production folders}/` | *validated* examples |
| `tests/component/Endpoints/OrderPaymentsEndpointTests.cs` | `tests/{App}.ComponentTests/{mirror of production folders}/` | example of a published-message check and a third-party failure; needs the Kafka/WireMock blocks |
| `tests/functional/FunctionalTestFixture.cs`, `FunctionalTestBase.cs` | `tests/{App}.FunctionalTests/` | *validated*; application resources |
| `tests/functional/AuthenticationTests.cs`, `Flows/OrderLifecycleFlowTests.cs` | `tests/{App}.FunctionalTests/` | *validated* examples |
| `tests/functional/Flows/OrderPaymentFlowTests.cs` | `tests/{App}.FunctionalTests/Flows/` | example needing the Kafka/WireMock blocks |
| `tests/unit/OrderPricingServiceTests.cs` | `tests/{App}.UnitTests/{mirror of production folders}/` | example |
| `tests/xunit.runner.json` | every component and functional project | *validated*; `CopyToOutputDirectory` |
| `tests/tests-README.md` | `tests/README.md` | fill and trim |

## Changes from the former skills' templates

- The test-only AppHost template (`apphost/AppHost.cs`, placed in `tests/orchestration/`) and `TestHostContract.cs` are
  replaced by `local/apphost/AppHost.cs`
  and `AppHostContract.cs` (one AppHost, `RunMode` instead of `TestMode`, contract owned by the AppHost).
- `ComponentAppFactory` no longer rewrites `JwtBearerOptions`: trust arrives as configuration through the mapping; the
  former code is kept as the strategy-C fallback in `references/local-jwt-authentication.md`.
- `TestTokenIssuer` signs HS256 or RS256 and creates invalid tokens; `TestAuthSettings` describes every application.
- `TestServerFixtureBase` waits for the shared initializer instead of applying the schema; its Kafka and WireMock
  members, and their resets in `PrepareForTestAsync`, are commented `ADAPT` blocks instead of active code.
- The component example was split: the validated `OrdersEndpointTests` covers response, database, validation and
  roles; `OrderPaymentsEndpointTests` keeps the message and third-party failure cases of the former example.
- New: `OutboundCallRecorder`, `AuthenticationTests` (component and functional), `LoginEndpointTests`, devtoken.
- `ExecuteCommandResult.ErrorMessage` (obsolete in Aspire 13.6) became `Message` in `FunctionalTestFixture`.
- The local-development template no longer says data is kept after `aspire stop` (it contradicted the ephemeral rule).

## Before trusting a template line

Package APIs move. The templates were written against Aspire 13.6.3, xunit.v3 4.0.2, Respawn 7.0.0, WireMock.Net 2.x,
Confluent.Kafka 2.x and Microsoft.IdentityModel 8.23.0. If the repository pins other versions and something does not
compile, check that package's current API instead of forcing the template: the intent of each member is documented in
its comments and in the references.
