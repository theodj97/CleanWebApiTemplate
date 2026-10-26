extern alias apphost;

using CleanWebApiTemplate.Tests.Shared.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using static CleanWebApiTemplate.AppHost.AppHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof(CleanWebApiTemplate.Api.ComponentTests.ComponentTestFixture))]

namespace CleanWebApiTemplate.Api.ComponentTests;

/// <summary>
/// Component tests: the shared AppHost starts infrastructure and schema only (Component mode); the application runs
/// in-process through <see cref="ComponentAppFactory"/>, so a test can still replace a DI service when needed.
/// </summary>
public sealed class ComponentTestFixture : TestServerFixtureBase
{
    private Dictionary<string, string?> _settings = [];

    public ComponentAppFactory Factory { get; private set; } = null!;

    public override string ApplicationResource => Resources.Api;

    protected override string Mode => Modes.Component;

    protected override Type AppHostEntryPoint => typeof(apphost::Projects.CleanWebApiTemplate_AppHost);

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        _settings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{AppSettings.PostgresConnectionName}"] =
                await RequiredConnectionStringAsync(Resources.PostgresDb, cancellationToken),
            // There is no appsettings.Testing.json: test-only values arrive as configuration, like the JWT trust below.
            // The API's appsettings.json logs at Trace for local development; a test run only keeps warnings and errors.
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
        };
        foreach (var (key, value) in TokenIssuer.TrustSettings(ApplicationResource))
        {
            _settings[key] = value;
        }

        // builder.Environment reads ASPNETCORE_ENVIRONMENT from the process in WebApplicationFactory, so the process
        // variable is the source that reliably selects the Testing environment for the in-process application.
        var previousEnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        using var restoreSettings = ApplyProcessEnvironment(_settings);
        try
        {
            Factory = new ComponentAppFactory(_settings);

            // Start the app now: startup errors surface in the fixture instead of in whichever test runs first.
            _ = Factory.Server;
            EnsureTestConfigurationIsEffective();
            await EnsureNoRemoteTokenMetadataAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironmentName);
        }
    }

    public override HttpClient CreateHttpClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public override async ValueTask DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    // A misspelled key would silently leave the appsettings value in place: a developer database, a real service or
    // a production Authority. Fail the whole run instead.
    private void EnsureTestConfigurationIsEffective()
    {
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();
        foreach (var (key, expected) in _settings)
        {
            if (configuration[key] != expected)
            {
                throw new InvalidOperationException(
                    $"The application does not read the test value for '{key}'. Check the key name and how Program.cs builds its configuration.");
            }
        }
    }

    // No JwtBearer scheme may download metadata or keys: no Authority, no MetadataAddress, no remote ConfigurationManager.
    private async Task EnsureNoRemoteTokenMetadataAsync()
    {
        var schemes = await Factory.Services.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();
        var options = Factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        foreach (var scheme in schemes.Where(s => s.HandlerType == typeof(JwtBearerHandler)))
        {
            var jwt = options.Get(scheme.Name);
            if (!string.IsNullOrEmpty(jwt.Authority) || !string.IsNullOrEmpty(jwt.MetadataAddress)
                || jwt.ConfigurationManager is not (null or StaticConfigurationManager<OpenIdConnectConfiguration>))
            {
                throw new InvalidOperationException(
                    $"JwtBearer scheme '{scheme.Name}' would download metadata from '{jwt.MetadataAddress ?? jwt.Authority}'. " +
                    "Clear it through AppHostContract.JwtTrust (TrustValue.Empty).");
            }
        }
    }

    private static IDisposable ApplyProcessEnvironment(IReadOnlyDictionary<string, string?> settings)
    {
        var previous = new Dictionary<string, string?>();
        foreach (var (key, value) in settings)
        {
            var name = ToEnvironmentVariable(key);
            previous[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        return new RestoreEnvironment(previous);
    }

    private static string ToEnvironmentVariable(string configurationKey) => configurationKey.Replace(":", "__");

    private sealed class RestoreEnvironment(Dictionary<string, string?> previous) : IDisposable
    {
        public void Dispose()
        {
            foreach (var (name, value) in previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
