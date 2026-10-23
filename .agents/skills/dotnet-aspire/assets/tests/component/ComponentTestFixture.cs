extern alias apphost;

using {{SharedNamespace}}.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using static {{AppHostNamespace}}.AppHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof({{ComponentTestsNamespace}}.ComponentTestFixture))]

namespace {{ComponentTestsNamespace}};

/// <summary>
/// Component tests: the shared AppHost starts infrastructure and schema only (Component mode); the application runs
/// in-process through <see cref="ComponentAppFactory"/>, so a test can still replace a DI service when needed.
/// </summary>
public sealed class ComponentTestFixture : TestServerFixtureBase
{
    private Dictionary<string, string?> _settings = [];

    public ComponentAppFactory Factory { get; private set; } = null!;

    public override string ApplicationResource => Resources.OrdersApi;

    protected override string Mode => Modes.Component;

    protected override Type AppHostEntryPoint => typeof(apphost::{{AppHostProjectType}});

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        // ADAPT: every value that points to infrastructure or a third party (discovery), with the AppHostContract keys,
        // e.g. [AppSettings.PaymentsApiBaseUrl] = App.GetEndpoint(Resources.WireMock, "http").ToString().TrimEnd('/'),
        // plus this run's token trust (AppHostContract.JwtTrust).
        _settings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{AppSettings.OrdersDbConnectionName}"] = await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken),
        };
        foreach (var (key, value) in TokenIssuer.TrustSettings(ApplicationResource))
        {
            _settings[key] = value;
        }

        Factory = new ComponentAppFactory(_settings);

        // Start the app now: startup errors surface in the fixture instead of in whichever test runs first.
        _ = Factory.Server;
        EnsureTestConfigurationIsEffective();
        await EnsureNoRemoteTokenMetadataAsync();
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
}
