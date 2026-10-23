using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{ComponentTestsNamespace}};

/// <summary>
/// Runs the real application in-process against the infrastructure the shared AppHost started (Component mode).
/// Token trust arrives as configuration, through the same keys the AppHost uses in the other modes
/// (AppHostContract.JwtTrust): the application's own validation code runs unchanged.
/// </summary>
public sealed class ComponentAppFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ADAPT: environment chosen in discovery. Not Development: dev-only behaviour (seeding, Swagger, exception
        // pages) stays off, as in production.
        builder.UseEnvironment("Testing");

        // Values read eagerly in Program.cs (before Build) only see UseSetting, and only when Program.cs calls
        // WebApplication.CreateBuilder(args). Values read later also see the in-memory source, which is added last.
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings));

        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.ConfigureTestServices(services =>
        {
            // ADAPT: background services that interfere with tests and are not under test, e.g.
            // services.RemoveHostedService<OutboxPublisherService>();

            // Safety net: any HttpClient from IHttpClientFactory that targets a non-local host fails fast.
            services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler(() => new BlockExternalHttpHandler()));
        });
    }
}
