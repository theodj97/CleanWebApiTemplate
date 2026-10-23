// Template: the single AppHost (local/aspire/{App}.AppHost/AppHost.cs), shared by `aspire run` and every test fixture.
// ADAPT: resources from the dependency inventory, applications from discovery, one JwtTrust mapping per protected
// application in AppHostContract. "Orders" (database + Jwt:Key) and "Catalog" (config-bound JwtBearer) are examples.
// Commented blocks are optional patterns: uncomment only what the inventory justifies. See references/apphost.md.
using Microsoft.Extensions.Configuration;
using System.Text;
using static {{AppHostNamespace}}.AppHostContract;

var builder = DistributedApplication.CreateBuilder(args);

// One AppHost for local development and tests. `aspire run` uses Local mode; the test fixtures pass
// "--RunMode=Component|Functional" through DistributedApplicationTestingBuilder. Every resource is declared once:
// the modes change configuration, never the shared topology.
var mode = builder.Configuration[ModeKey] ?? Modes.Local;
if (mode is not (Modes.Local or Modes.Component or Modes.Functional))
{
    throw new InvalidOperationException($"Unknown {ModeKey} '{mode}'.");
}

var isTest = mode != Modes.Local;
var outboundProxy = builder.Configuration[OutboundProxyKey] ?? "http://127.0.0.1:9";

// Ephemeral by design in every mode: no data volumes, no persistent lifetime. Each start begins with empty containers.
var postgres = builder.AddPostgres(Resources.Postgres);
var ordersDb = postgres.AddDatabase(Resources.OrdersDb);

// ADAPT (only when the inventory has them): more infrastructure, always the production engine, never a volume.
// var cache = builder.AddRedis(Resources.Redis);
// var kafka = builder.AddKafka(Resources.Kafka);

// ADAPT (third-party HTTP API): the real service with a secret parameter in Local mode, WireMock in test modes.
// var paymentsUrl = isTest
//     ? builder.AddWireMock(Resources.WireMock).GetEndpoint("http")          // package WireMock.Net.Aspire
//     : null;
// var paymentsApiKey = isTest ? null : builder.AddParameter("payments-api-key", secret: true);

// Schema on every start, in every mode. The synthetic seed runs only in Local mode: tests start from empty tables.
// ADAPT: if the application migrates itself on startup, never add a second runner (references/database.md,
// "When the application migrates itself").
var dbInitializer = builder.AddProject<{{DbInitializerProjectType}}>(Resources.DbInitializer)
    .WithReference(ordersDb, connectionName: AppSettings.OrdersDbConnectionName)
    .WaitFor(ordersDb);

if (!isTest)
{
    dbInitializer.WithEnvironment(
        "Seed__Path", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../database/seed")));
}
else
{
    // No telemetry in tests: ServiceDefaults only enables the OTLP exporter when this value is not empty.
    dbInitializer.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty);
}

// Component mode stops here: the API under test runs in-process inside the component tests.
if (mode != Modes.Component)
{
    if (!isTest)
    {
        // Issuer, algorithm and audiences shared with the devtoken tool. Keys are not here: they live in the AppHost
        // user secrets, outside the repository (`dotnet local/tools/devtoken.cs init`).
        builder.Configuration.AddJsonFile(Path.Combine(builder.AppHostDirectory, "../../tools/devtoken.json"), optional: false);
    }

    var localJwt = LocalJwt.Create(builder, requireKeys: !isTest);

    AddApplication<{{OrdersApiProjectType}}>(Resources.OrdersApi)
        .WithReference(ordersDb, connectionName: AppSettings.OrdersDbConnectionName)
        .WaitFor(ordersDb)
        .WaitForCompletion(dbInitializer)
        .WithLocalJwtTrust(localJwt, Resources.OrdersApi);

    AddApplication<{{CatalogApiProjectType}}>(Resources.CatalogApi)
        .WithLocalJwtTrust(localJwt, Resources.CatalogApi);

    // ADAPT: configuration under the exact keys the application reads (discovery), e.g.
    //     .WithReference(cache).WaitFor(cache)
    //     .WithEnvironment(ToEnvironmentVariable("Kafka:BootstrapServers"), kafka.Resource.ConnectionStringExpression).WaitFor(kafka)
    //     .WithEnvironment(ToEnvironmentVariable("PaymentsApi:BaseUrl"), paymentsUrl) in test modes, a parameter in Local.
    // ADAPT (strategy C only, after asking): a loopback identity stub, references/local-jwt-authentication.md.
}

builder.Build().Run();

IResourceBuilder<ProjectResource> AddApplication<TProject>(string name) where TProject : IProjectMetadata, new()
{
    if (!isTest)
    {
        // Local: the project's launch profile (Development), dashboard telemetry and its health endpoint.
        // ADAPT: WithHttpHealthCheck only when the application maps that path in Development (ServiceDefaults does).
        return builder.AddProject<TProject>(name).WithHttpHealthCheck("/health");
    }

    return builder.AddProject<TProject>(name, launchProfileName: null)
        // HTTP only: no dev-certificate trust needed; Aspire assigns the port.
        .WithHttpEndpoint()
        // ADAPT: environment chosen in discovery; add .WithHttpHealthCheck(<path>) only if the application maps a health
        // endpoint in that environment (otherwise the functional fixture polls for HTTP readiness).
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        // No telemetry in tests: ServiceDefaults only enables the OTLP exporter when this value is not empty.
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty)
        // Safety net: any HTTP call to a non-local host goes to this proxy (a dead port unless the fixture records
        // calls) instead of reaching a real service or identity provider.
        .WithEnvironment("HTTP_PROXY", outboundProxy)
        .WithEnvironment("HTTPS_PROXY", outboundProxy)
        .WithEnvironment("NO_PROXY", "localhost,127.0.0.1")
        // The functional fixture starts each application after the schema exists.
        .WithExplicitStart();
}

/// <summary>Local JWT trust material for the current mode: never production values.</summary>
internal sealed record LocalJwt(
    string Algorithm,
    string Issuer,
    IResourceBuilder<ParameterResource>? SigningKey,
    IResourceBuilder<ParameterResource>? PublicKey,
    IConfiguration Configuration)
{
    public static LocalJwt Create(IDistributedApplicationBuilder builder, bool requireKeys)
    {
        var configuration = builder.Configuration;
        var algorithm = configuration[LocalAuth.AlgorithmKey] ?? "HS256";
        var issuer = configuration[LocalAuth.IssuerKey]
            ?? throw new InvalidOperationException($"Missing '{LocalAuth.IssuerKey}'.");

        var parameter = algorithm switch
        {
            "HS256" => LocalAuth.SigningKeyParameter,
            "RS256" => LocalAuth.PublicKeyParameter,
            _ => throw new InvalidOperationException($"Unsupported local JWT algorithm '{algorithm}'."),
        };

        if (requireKeys && string.IsNullOrWhiteSpace(configuration[LocalAuth.ParameterKey(parameter)]))
        {
            throw new InvalidOperationException(
                "The local JWT signing key is missing. Run `dotnet local/tools/devtoken.cs init` from the repository root, then start the AppHost again.");
        }

        return algorithm == "HS256"
            ? new(algorithm, issuer, builder.AddParameter(parameter, secret: true), null, configuration)
            : new(algorithm, issuer, null, builder.AddParameter(parameter), configuration);
    }

    public string AudienceFor(string resourceName) =>
        Configuration[LocalAuth.AudienceKey(resourceName)]
        ?? throw new InvalidOperationException($"Missing '{LocalAuth.AudienceKey(resourceName)}'.");
}

internal static class LocalJwtResourceExtensions
{
    /// <summary>Applies <see cref="AppHostContract.JwtTrust"/> as environment variables.</summary>
    public static IResourceBuilder<ProjectResource> WithLocalJwtTrust(
        this IResourceBuilder<ProjectResource> application, LocalJwt localJwt, string resourceName)
    {
        foreach (var (key, value) in JwtTrust(resourceName))
        {
            var name = ToEnvironmentVariable(key);
            switch (value)
            {
                case TrustValue.Issuer:
                    application.WithEnvironment(name, localJwt.Issuer);
                    break;
                case TrustValue.Audience:
                    application.WithEnvironment(name, localJwt.AudienceFor(resourceName));
                    break;
                case TrustValue.SigningKeyText:
                    application.WithEnvironment(name, Required(localJwt.SigningKey, value));
                    break;
                case TrustValue.SigningKeyBase64:
                    var signingKey = Required(localJwt.SigningKey, value);
                    application.WithEnvironment(async context =>
                    {
                        var text = await signingKey.Resource.GetValueAsync(context.CancellationToken) ?? string.Empty;
                        context.EnvironmentVariables[name] = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    });
                    break;
                case TrustValue.PublicKeyPem:
                    application.WithEnvironment(name, Required(localJwt.PublicKey, value));
                    break;
                case TrustValue.Empty:
                    application.WithEnvironment(name, string.Empty);
                    break;
            }
        }

        return application;
    }

    private static IResourceBuilder<ParameterResource> Required(IResourceBuilder<ParameterResource>? parameter, TrustValue value) =>
        parameter ?? throw new InvalidOperationException($"{value} needs a different LocalAuth:Algorithm.");
}
