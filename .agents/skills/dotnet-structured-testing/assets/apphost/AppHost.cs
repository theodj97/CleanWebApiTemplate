// Test-only Aspire AppHost: orchestrates the environment the tests run against. It is not the development AppHost
// and is never deployed.
// - No dashboard, no ServiceDefaults, no OpenTelemetry: tests need containers, not observability.
// - No data volumes and no persistent container lifetime: every test session starts from empty containers that
//   live exactly as long as the session.
// - "--TestMode=Component" starts infrastructure only; "--TestMode=Functional" also declares the application(s).
// TestHostContract is linked from the Shared project (see its remarks).
using static {{SharedNamespace}}.TestHostContract;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DisableDashboard = true,
});

var mode = builder.Configuration[ModeKey] ?? Modes.Component;

// ADAPT: one resource per dependency found in discovery, always the same engine/broker as production.
var postgres = builder.AddPostgres(Resources.Postgres);
var ordersDb = postgres.AddDatabase(Resources.OrdersDb);
var kafka = builder.AddKafka(Resources.Kafka);
var wiremock = builder.AddWireMock(Resources.WireMock);

if (mode == Modes.Functional)
{
    var identity = builder.AddWireMock(Resources.Identity);

    // ADAPT: one AddProject per deployable application in the flow under test.
    builder.AddProject<{{AppProjectType}}>(Resources.OrdersApi, launchProfileName: null)
        // HTTP only: no dev-certificate trust needed and HTTPS redirection stays inactive.
        .WithHttpEndpoint()
        // ADAPT: health endpoint, only if the app maps it in the environment used below (ServiceDefaults maps
        // /health only in Development): a check that never passes makes the fixture wait until its timeout.
        // .WithHttpHealthCheck("/health")
        // ADAPT: environment chosen in discovery (avoid Development-only behaviour unless the app needs it).
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        // Configuration under the exact names the app reads (discovery section 4).
        .WithReference(ordersDb, connectionName: AppSettings.OrdersDbConnectionName)
        .WithEnvironment(ToEnvironmentVariable(AppSettings.KafkaBootstrapServers), kafka.Resource.ConnectionStringExpression)
        .WithEnvironment(ToEnvironmentVariable(AppSettings.PaymentsApiBaseUrl), wiremock.GetEndpoint("http"))
        .WithEnvironment(ToEnvironmentVariable(AppSettings.JwtAuthority), identity.GetEndpoint("http"))
        .WithEnvironment(ToEnvironmentVariable(AppSettings.JwtRequireHttpsMetadata), "false")
        // No telemetry in tests: ServiceDefaults only enables the OTLP exporter when this value is not empty.
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty)
        // Safety net: an HTTP call to any non-local host fails fast instead of reaching a real service.
        .WithEnvironment("HTTP_PROXY", "http://127.0.0.1:9")
        .WithEnvironment("HTTPS_PROXY", "http://127.0.0.1:9")
        .WithEnvironment("NO_PROXY", "localhost,127.0.0.1")
        .WaitFor(ordersDb)
        .WaitFor(kafka)
        .WaitFor(wiremock)
        .WaitFor(identity)
        // The functional fixture starts the app itself, after the schema, topics and identity stubs exist.
        .WithExplicitStart();
}

builder.Build().Run();
