using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Fixtures;

namespace {{ComponentTestsNamespace}};

/// <summary>Base class of every component test class (see TestBase for the per-test lifecycle).</summary>
public abstract class ComponentTestBase(ComponentTestFixture fixture) : TestBase<ComponentTestFixture>(fixture)
{
    private readonly List<WebApplicationFactory<Program>> _customApplications = []; // ADAPT: the app's entry point type

    /// <summary>
    /// Client for a separate application instance with replaced services. Use it only when a scenario cannot be
    /// provoked through the public entry points or WireMock (a fixed clock, an exception from an internal dependency).
    /// It builds and starts a second app, so keep these tests few, and remove in <paramref name="replaceServices"/> the
    /// hosted message consumers of the app (RemoveHostedService): a second consumer in the same group triggers a
    /// rebalance that the main app's consumers need time to recover from.
    /// </summary>
    protected HttpClient CreateClientWithServices(Action<IServiceCollection> replaceServices, TestUser? user = null)
    {
        var application = Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(replaceServices));
        _customApplications.Add(application);

        var client = Track(application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));
        return user is { } identity ? Authorize(client, identity) : client;
    }

    public override async ValueTask DisposeAsync()
    {
        // Clients first (base), then the application instances they belong to.
        await base.DisposeAsync();

        foreach (var application in _customApplications)
        {
            await application.DisposeAsync();
        }
    }
}
