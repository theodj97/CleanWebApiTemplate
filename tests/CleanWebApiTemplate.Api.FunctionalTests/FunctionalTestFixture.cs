extern alias apphost;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using CleanWebApiTemplate.Tests.Shared.Fixtures;
using CleanWebApiTemplate.Tests.Shared.Http;
using CleanWebApiTemplate.Tests.Shared.Waiting;
using Microsoft.Extensions.DependencyInjection;
using static CleanWebApiTemplate.AppHost.AppHostContract;

// One environment for the whole test project: started before the first test, disposed after the last one.
[assembly: AssemblyFixture(typeof(CleanWebApiTemplate.Api.FunctionalTests.FunctionalTestFixture))]

namespace CleanWebApiTemplate.Api.FunctionalTests;

/// <summary>
/// Functional tests: the shared AppHost in Functional mode runs the application as a separate process on a dynamic
/// port, trusting this run's local issuer through environment variables only. Black box: HTTP only.
/// </summary>
public sealed class FunctionalTestFixture : TestServerFixtureBase
{
    private static readonly string[] ApplicationResources = [Resources.Api];

    public override string ApplicationResource => Resources.Api;

    protected override string Mode => Modes.Functional;

    protected override Type AppHostEntryPoint => typeof(apphost::Projects.CleanWebApiTemplate_AppHost);

    /// <summary>Every outbound HTTP(S) call of the application, recorded and refused. Tests assert it stays empty.</summary>
    public OutboundCallRecorder OutboundCalls { get; } = new();

    protected override IEnumerable<string> AdditionalAppHostArguments => [$"--{OutboundProxyKey}={OutboundCalls.ProxyUrl}"];

    protected override async Task StartApplicationAsync(CancellationToken cancellationToken)
    {
        // The apps are declared WithExplicitStart(): start them now that the schema exists.
        var model = App.Services.GetRequiredService<DistributedApplicationModel>();
        foreach (var name in ApplicationResources)
        {
            var resource = model.Resources.Single(r => r.Name == name);
            var result = await App.ResourceCommands.ExecuteCommandAsync(resource, KnownResourceCommands.StartCommand, cancellationToken);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not start '{name}': {result.Message}");
            }

            await App.ResourceNotifications.WaitForResourceHealthyAsync(name, cancellationToken);
        }
    }

    // Without a health check in Functional mode, "healthy" only means "running": wait until the application answers
    // HTTP before the first test. /health/api is mapped in every environment and, unlike /health, does not depend on
    // the database check, so it only reports whether the process is serving requests.
    protected override async Task WaitUntilApplicationIsReadyAsync(CancellationToken cancellationToken)
    {
        foreach (var name in ApplicationResources)
        {
            using var client = CreateHttpClient(name);
            var answered = await Eventually.GetAsync<HttpResponseMessage>(
                async () => await client.GetAsync("/health/api", cancellationToken),
                response => response.IsSuccessStatusCode,
                timeout: TimeSpan.FromMinutes(1));
            if (answered is null)
            {
                throw new InvalidOperationException($"'{name}' did not answer HTTP requests at /health/api.");
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
