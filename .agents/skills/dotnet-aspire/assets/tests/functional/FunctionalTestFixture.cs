extern alias apphost;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using {{SharedNamespace}}.Fixtures;
using {{SharedNamespace}}.Http;
using {{SharedNamespace}}.Waiting;
using Microsoft.Extensions.DependencyInjection;
using static {{AppHostNamespace}}.AppHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof({{FunctionalTestsNamespace}}.FunctionalTestFixture))]

namespace {{FunctionalTestsNamespace}};

/// <summary>
/// Functional tests: the shared AppHost in Functional mode runs the application(s) as separate processes on dynamic
/// ports, trusting this run's local issuer through environment variables only. Black box: HTTP and messages only.
/// </summary>
public sealed class FunctionalTestFixture : TestServerFixtureBase
{
    /// <summary>ADAPT: every application resource of the flows, in start order.</summary>
    private static readonly string[] ApplicationResources = [Resources.OrdersApi];

    public override string ApplicationResource => Resources.OrdersApi;

    protected override string Mode => Modes.Functional;

    protected override Type AppHostEntryPoint => typeof(apphost::{{AppHostProjectType}});

    /// <summary>Every outbound HTTP(S) call of the applications, recorded and refused. Tests assert it stays empty.</summary>
    public OutboundCallRecorder OutboundCalls { get; } = new();

    protected override IEnumerable<string> AdditionalAppHostArguments => [$"--{OutboundProxyKey}={OutboundCalls.ProxyUrl}"];

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        // ADAPT (strategy C only): register the loopback identity stub before starting the applications:
        // await IdentityProviderStub.RegisterAsync(App.CreateWireMockAdminClient(Resources.Identity),
        //     App.GetEndpoint(Resources.Identity, "http").ToString(), TokenIssuer);

        // The apps are declared WithExplicitStart(): start them now that the schema exists.
        var model = App.Services.GetRequiredService<DistributedApplicationModel>();
        foreach (var name in ApplicationResources)
        {
            var resource = model.Resources.Single(r => r.Name == name);
            // Verify against the installed Aspire version: WithExplicitStart, ResourceCommands and
            // WithReference(..., connectionName) are recent APIs; adapt rather than downgrade the design.
            var result = await App.ResourceCommands.ExecuteCommandAsync(resource, KnownResourceCommands.StartCommand, cancellationToken);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not start '{name}': {result.Message}");
            }

            await App.ResourceNotifications.WaitForResourceHealthyAsync(name, cancellationToken);
        }
    }

    // ADAPT: without a health check in Functional mode (e.g. ServiceDefaults maps /health only in Development) "healthy"
    // only means "running": wait until each application answers HTTP before the first test.
    protected override async Task WaitUntilApplicationIsReadyAsync(CancellationToken cancellationToken)
    {
        foreach (var name in ApplicationResources)
        {
            using var client = CreateHttpClient(name);
            var answered = await Eventually.GetAsync<HttpResponseMessage>(
                async () => await client.GetAsync("/", cancellationToken), timeout: TimeSpan.FromMinutes(1));
            if (answered is null)
            {
                throw new InvalidOperationException($"'{name}' did not answer HTTP requests.");
            }
        }
    }

    public override HttpClient CreateHttpClient() => CreateHttpClient(ApplicationResource);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await OutboundCalls.DisposeAsync();
    }

    /// <summary>Client for any application resource (flows that span several apps).</summary>
    public HttpClient CreateHttpClient(string resourceName) => App.CreateHttpClient(resourceName, "http");
}
