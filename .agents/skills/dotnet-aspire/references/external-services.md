# External services: dependency → substitute

Canonical for SKILL.md rule 8. Infrastructure dependencies run as real containers (or official emulators) in every
mode, and the tests run against them. Only third-party HTTP/gRPC APIs are simulated, and only in the test modes. This
keeps the code paths that break in production — SQL dialect, transactions, serialization, broker routing — inside the
tests.

## Contents
- [Local mode and test modes](#local-mode-and-test-modes)
- [Substitution table](#substitution-table)
- [Nothing leaves the machine](#nothing-leaves-the-machine)
- [WireMock for third-party APIs](#wiremock-for-third-party-apis)
- [SMTP with MailPit](#smtp-with-mailpit)
- [Cloud services](#cloud-services)
- [Dependencies not in the table](#dependencies-not-in-the-table)

## Local mode and test modes

The shared AppHost declares each dependency once and changes only what each mode needs:

| Dependency | Local mode | Component / Functional modes |
|---|---|---|
| Database, cache, broker, storage | container or official emulator | the same container or emulator |
| Third-party HTTP/gRPC API | the real service with credentials from secret parameters, or an official sandbox/emulator; documented in `local/local-development.md` | a WireMock resource declared only in test modes, the app's base URL setting pointing to it |
| SMTP | MailPit container (developers read mails in its UI) | MailPit container |
| Identity provider | never: locally signed JWTs (a strategy-C loopback stub only after consent) | never: locally signed JWTs (same exception) |
| Cloud configuration providers (Key Vault, App Configuration) | disabled through the app's existing condition, or parameters | disabled through the app's existing condition |

Never fake a service that cannot reasonably be emulated in Local mode: list it and what does not work without its
credentials.

## Substitution table

Package and API names move between Aspire releases: verify them against the Aspire version in use.

| Dependency | Substitute (AppHost) | Reset between tests | Verify in Assert |
|---|---|---|---|
| SQL Server | `AddSqlServer(...).AddDatabase(...)` (Aspire.Hosting.SqlServer) | Respawn | test repositories |
| PostgreSQL | `AddPostgres(...).AddDatabase(...)` (Aspire.Hosting.PostgreSQL) | Respawn | test repositories |
| MySQL / MariaDB | `AddMySql(...).AddDatabase(...)` (Aspire.Hosting.MySql) | Respawn | test repositories |
| Oracle | `AddOracle(...).AddDatabase(...)` (Aspire.Hosting.Oracle) — heavy image, long first start | Respawn | test repositories |
| MongoDB | `AddMongoDB(...).AddDatabase(...)` (Aspire.Hosting.MongoDB) | `MongoDatabaseResetter` (listed collections) | test repositories |
| Redis | `AddRedis(...)` (Aspire.Hosting.Redis) | `RedisDatabaseResetter` (configured key prefixes) | read keys through a test connection |
| Kafka | `AddKafka(...)` (Aspire.Hosting.Kafka) | offset snapshot per test | `KafkaTestHelper` |
| RabbitMQ | `AddRabbitMQ(...)` (Aspire.Hosting.RabbitMQ) | purge app queues | probe queue bound before the Act |
| Azure Service Bus | `AddAzureServiceBus(...).RunAsEmulator()` + entities declared in the AppHost | drain test subscription | test subscription/receiver |
| Azure Storage | `AddAzureStorage(...).RunAsEmulator()` (Azurite) | delete blobs/messages/entities | storage SDK against the emulator |
| Cosmos DB | `AddAzureCosmosDB(...).RunAsEmulator()` (or the preview Linux emulator) | delete items | Cosmos SDK against the emulator |
| AWS S3/SQS/SNS/DynamoDB | LocalStack container (community integration or `AddContainer`) | delete objects/messages/items | AWS SDK with the LocalStack endpoint |
| Third-party HTTP APIs | WireMock.Net: `AddWireMock("wiremock")` (WireMock.Net.Aspire) | reset mappings + request log | `Fixture.WireMock.GetRequestsAsync` |
| Third-party gRPC APIs | WireMock.Net with `WithProtoDefinition(...)` (HTTP/2) | same as HTTP | request log |
| SMTP | MailPit container (CommunityToolkit.Aspire.Hosting.MailPit `AddMailPit`) | `DELETE /api/v1/messages` | MailPit HTTP API |
| Identity provider | never substituted by a provider: locally signed JWTs (local-jwt-authentication.md) | — | — |

Never use SQLite or EF Core InMemory in place of a database engine: different transactions, types, constraints and SQL
hide real errors. Never use an in-memory broker transport in place of the broker.

LocalStack: check the current licensing/auth-token requirements of the LocalStack image before choosing it. If it is
not usable, propose single-service alternatives (MinIO for S3, ElasticMQ for SQS, DynamoDB Local) and ask.

## Nothing leaves the machine

No test may reach a real service, and no real URL may remain configured.

1. **Override every URL** found in discovery (appsettings of every environment, code defaults, launch profiles) with a
   local substitute, using the app's exact keys (`AppHostContract.AppSettings`): AppHost `WithEnvironment(...)` for
   functional tests, factory settings for component tests.
2. **Aspire service discovery** (`https+http://catalog`): configure `services:catalog:http:0` (env
   `services__catalog__http__0`) to the substitute's endpoint.
3. **Configuration providers that call the cloud at startup** (Key Vault, App Configuration): disable them through
   the condition the app already has (environment, flag). If they are unconditional, ask — skipping them needs a
   production change.
4. **Safety nets** that make a missed URL fail fast with a clear message:
   - component: `BlockExternalHttpHandler` added to every `IHttpClientFactory` client via
     `ConfigureHttpClientDefaults` (allows loopback, `*.localhost` and service-discovery names);
   - functional: `HTTP_PROXY`/`HTTPS_PROXY` on the app resources, read by the AppHost from `Testing:OutboundProxy`
     (default `http://127.0.0.1:9`, a dead port), with `NO_PROXY=localhost,127.0.0.1`. The functional fixture points
     it to an `OutboundCallRecorder`, which refuses and records each call so tests can assert that none happened (a
     dead port alone hides non-fatal calls, such as a JwtBearer metadata download). .NET's `HttpClient` honours these
     variables on every OS; they do not affect database or broker connections.
   A safety net firing is a configuration bug to fix in the mapping, never a reason to relax the net.

## WireMock for third-party APIs

AppHost: `builder.AddWireMock(AppHostContract.Resources.WireMock)` (test modes only); the app's base URL setting points to
`wiremock.GetEndpoint("http")` (functional) or `App.GetEndpoint("wiremock", "http")` (component). Tests get the admin
client from `App.CreateWireMockAdminClient("wiremock")`, wrapped by `Fixture.WireMock` (`WireMockTestHelper`).

- **Reset before every test** (mappings and request log) — done by `PrepareForTestAsync`.
- **Stubs in Arrange**, only the ones the test needs:
  ```csharp
  // Arrange
  await Fixture.WireMock.StubJsonAsync("POST", "/v1/payments", statusCode: 201, body: new { id = "pay_1", status = "approved" });
  ```
  For matching on body/headers/query or for sequences, build a `MappingModel` (or use `Admin.GetMappingBuilder()`)
  and pass it to `StubAsync`.
- **Verification in Assert** through the request log:
  ```csharp
  // Assert
  var calls = await Fixture.WireMock.GetRequestsAsync("POST", "/v1/payments");
  var call = Assert.Single(calls);
  Assert.Contains("\"amount\":100", call.Request.Body);
  ```
- **Failures**: 4xx/5xx status codes and `delay` beyond the client's timeout. Resilience handlers (Polly,
  `AddStandardResilienceHandler`) retry: assert the number of calls the policy implies, or `NotEmpty`, not `Single`.
- Static mapping files (`WithMappingsPath`) only for stubs every test needs; per-test stubs keep each test readable on
  its own.

## SMTP with MailPit

AppHost: MailPit resource (SMTP port for the app, HTTP API for tests). Point the app's SMTP host/port settings to it.
- Reset: `DELETE /api/v1/messages` in `PrepareForTestAsync`.
- Verify: `GET /api/v1/search?query=to:"customer@example.com"` or `GET /api/v1/messages`, then
  `GET /api/v1/message/{ID}` for the body, wrapped in `Eventually.AssertAsync` (delivery is asynchronous).

## Cloud services

- Use the official emulators through Aspire's `RunAsEmulator()`; in run mode no cloud resource is provisioned.
- Connection strings come from `App.GetConnectionStringAsync(name)` (component) or `WithReference` (functional).
- Managed-identity or `DefaultAzureCredential` code paths: the emulators use connection strings/keys; make sure the
  app picks the connection-string path through configuration. If it can only use a credential, ask.

## Dependencies not in the table

Propose a substitute (prefer a real container of the same product; then an official emulator; then a protocol-level
fake such as WireMock) with its trade-offs, and ask before generating. Replacing a dependency with a DI fake is allowed
only in component tests, only for scenarios that cannot be provoked otherwise, and never in functional tests.
