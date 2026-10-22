using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using {{SharedNamespace}}.Fixtures;
using static {{SharedNamespace}}.TestHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof({{ComponentTestsNamespace}}.ComponentTestFixture))]

namespace {{ComponentTestsNamespace}};

/// <summary>
/// Component tests: Aspire starts only the infrastructure; the application runs in-process through
/// <see cref="ComponentAppFactory"/>, so a test can still replace a DI service when a scenario requires it.
/// </summary>
public sealed class ComponentTestFixture : TestServerFixtureBase
{
    private IReadOnlyDictionary<string, string?> _settings = new Dictionary<string, string?>();

    public ComponentAppFactory Factory { get; private set; } = null!;

    protected override string Mode => Modes.Component;

    protected override Type AppHostEntryPoint => typeof({{AppHostProjectType}});

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        // ADAPT: every configuration value that points to infrastructure or to a third party (discovery section 4),
        // with the same keys the AppHost injects in functional mode.
        _settings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{AppSettings.OrdersDbConnectionName}"] =
                await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken),
            [AppSettings.KafkaBootstrapServers] = await RequiredConnectionStringAsync(Resources.Kafka, cancellationToken),
            [AppSettings.PaymentsApiBaseUrl] = App.GetEndpoint(Resources.WireMock, "http").ToString().TrimEnd('/'),
        };

        Factory = new ComponentAppFactory(_settings, TokenIssuer);

        // Start the app now: startup errors surface in the fixture instead of in whichever test runs first.
        _ = Factory.Server;
        EnsureTestConfigurationIsEffective();
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

    // A misspelled key would silently leave the appsettings value in place, i.e. a developer database or a real
    // service. Fail the whole run instead.
    private void EnsureTestConfigurationIsEffective()
    {
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();

        foreach (var (key, expected) in _settings)
        {
            if (configuration[key] != expected)
            {
                throw new InvalidOperationException(
                    $"The application reads '{key}' = '{configuration[key]}' instead of the test value. " +
                    "Check the key name and how Program.cs builds its configuration (references/component-tests.md).");
            }
        }
    }
}
