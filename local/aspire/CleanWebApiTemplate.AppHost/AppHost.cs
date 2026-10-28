using System.Security.Cryptography;
using System.Text;
using CleanWebApiTemplate.AppHost;
using Microsoft.Extensions.Configuration;
using static CleanWebApiTemplate.AppHost.AppHostContract;

// One AppHost for local development and, in the future, the test projects.
// `aspire run` uses Local mode; test fixtures pass "--RunMode=Component|Functional".
var builder = DistributedApplication.CreateBuilder(args);

var mode = builder.Configuration[ModeKey] ?? Modes.Local;
if (mode is not (Modes.Local or Modes.Component or Modes.Functional))
{
    throw new InvalidOperationException($"Unknown {ModeKey} '{mode}'.");
}

var isTest = mode != Modes.Local;
var outboundProxy = builder.Configuration[OutboundProxyKey] ?? "http://127.0.0.1:9";

// Ephemeral by design in every mode: no data volumes, no persistent lifetime.
// Every start begins with empty containers, rebuilds the schema (and the seed in Local mode) and reseeds the secrets.
var postgres = builder.AddPostgres(Resources.Postgres);
var todoDb = postgres.AddDatabase(Resources.PostgresDb);

var dbInitializer = builder.AddProject<Projects.CleanWebApiTemplate_DbInitializer>(Resources.DbInitializer)
    .WithReference(todoDb, connectionName: AppSettings.PostgresConnectionName)
    .WaitFor(todoDb)
    .WithEnvironment("Schema__Path", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../database")));

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

// Issuer and audience shared with the devtoken tool. Keys are not here: they live in the AppHost
// user secrets, outside the repository (`dotnet local/tools/devtoken.cs init`). Test modes pass the same values
// as arguments, so the file is only read in Local mode.
if (!isTest)
{
    builder.Configuration.AddJsonFile(
        Path.Combine(builder.AppHostDirectory, "../../tools/devtoken.json"), optional: false);
}

var localJwt = LocalJwt.Create(builder, requireKeys: !isTest);

// The applications never receive the signing key: it is seeded into OpenBao in every mode. The root token is
// generated per start, so nothing is persisted and nothing is committed. A test can supply its own through
// Parameters:openbao-root-token.
var openBaoToken = builder.Configuration[OpenBao.RootTokenParameterKey]
                   ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var openBao = builder.AddOpenBao(openBaoToken);
var openBaoInitializer = builder.AddOpenBaoInitializer(openBao, openBaoToken, OpenBao.SecretsPath(mode), localJwt);

// Component mode stops here: the API under test runs in-process inside the component tests.
if (mode != Modes.Component)
{
    AddApplication<Projects.CleanWebApiTemplate_Api>(Resources.Api)
        .WithOpenBaoSecrets(openBao, openBaoToken, OpenBao.SecretsPath(mode))
        .WithReference(todoDb, connectionName: AppSettings.PostgresConnectionName)
        .WaitFor(todoDb)
        .WaitFor(openBao)
        .WaitForCompletion(openBaoInitializer)
        .WaitForCompletion(dbInitializer)
        .WithLocalJwtTrust(localJwt, Resources.Api);
}

builder.Build().Run();

IResourceBuilder<ProjectResource> AddApplication<TProject>(string name) where TProject : IProjectMetadata, new()
{
    if (!isTest)
    {
        // Local: the project's launch profile (Development, HTTPS), dashboard telemetry and its health endpoint.
        // WithHttpHealthCheck is scheme-agnostic in Aspire 13: it follows the endpoint the profile exposes.
        return builder.AddProject<TProject>(name, launchProfileName: "Development").WithHttpHealthCheck("/health");
    }

    return builder.AddProject<TProject>(name, launchProfileName: null)
        // HTTP only: no development-certificate trust needed; Aspire assigns the port.
        .WithHttpEndpoint()
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        // No appsettings.Testing.json: the test-only values arrive as environment variables.
        .WithEnvironment("Logging__LogLevel__Default", "Warning")
        .WithEnvironment("Logging__LogLevel__Microsoft", "Warning")
        .WithEnvironment("Logging__LogLevel__Microsoft.Hosting.Lifetime", "Warning")
        // No telemetry in tests.
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty)
        // Safety net: outbound HTTP calls go to a dead proxy instead of reaching a real service.
        .WithEnvironment("HTTP_PROXY", outboundProxy)
        .WithEnvironment("HTTPS_PROXY", outboundProxy)
        .WithEnvironment("NO_PROXY", "localhost,127.0.0.1")
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
