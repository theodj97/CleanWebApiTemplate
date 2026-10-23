using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Api.ComponentTests;

/// <summary>Base class of every component test class (see TestBase for the per-test lifecycle).</summary>
public abstract class ComponentTestBase(ComponentTestFixture fixture) : TestBase<ComponentTestFixture>(fixture)
{
    private readonly List<WebApplicationFactory<Program>> _customApplications = [];

    /// <summary>
    /// Client for a separate application instance with replaced services. Use it only when a scenario cannot be
    /// provoked through the public entry points. It builds and starts a second app, so keep these tests few.
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
