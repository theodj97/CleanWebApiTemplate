using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Fixtures;
using static {{SharedNamespace}}.TestHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof({{FunctionalTestsNamespace}}.FunctionalTestFixture))]

namespace {{FunctionalTestsNamespace}};

/// <summary>
/// Functional tests: Aspire runs the whole system, including the application(s) as separate processes. Tests are
/// black box: HTTP and messages only, no access to the application's DI container. Arrange may still seed the
/// database through the test repositories.
/// </summary>
public sealed class FunctionalTestFixture : TestServerFixtureBase
{
    /// <summary>ADAPT: every application resource of the flow, in start order.</summary>
    private static readonly string[] ApplicationResources = [Resources.OrdersApi];

    protected override string Mode => Modes.Functional;

    protected override Type AppHostEntryPoint => typeof({{AppHostProjectType}});

    protected override IReadOnlyList<string> InfrastructureResources => [.. base.InfrastructureResources, Resources.Identity];

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        // 1. The app must be able to fetch the discovery document and JWKS before it validates its first token.
        await IdentityProviderStub.RegisterAsync(
            App.CreateWireMockAdminClient(Resources.Identity),
            App.GetEndpoint(Resources.Identity, "http").ToString(),
            TokenIssuer);

        // 2. The apps are declared WithExplicitStart(): start them now that schema, topics and stubs exist.
        var model = App.Services.GetRequiredService<DistributedApplicationModel>();
        foreach (var name in ApplicationResources)
        {
            var resource = model.Resources.Single(r => r.Name == name);
            // Verify against the installed Aspire version: ResourceCommands and KnownResourceCommands are recent APIs.
            var result = await App.ResourceCommands.ExecuteCommandAsync(resource, KnownResourceCommands.StartCommand, cancellationToken);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not start '{name}': {result.ErrorMessage}");
            }

            // Healthy = health check passing when the AppHost declares WithHttpHealthCheck, otherwise just running.
            await App.ResourceNotifications.WaitForResourceHealthyAsync(name, cancellationToken);
        }
    }

    /// <summary>Client for the main application of the flow.</summary>
    public override HttpClient CreateHttpClient() => CreateHttpClient(Resources.OrdersApi);

    /// <summary>Client for any application resource (flows that span several apps).</summary>
    public HttpClient CreateHttpClient(string resourceName) => App.CreateHttpClient(resourceName, "http");
}
