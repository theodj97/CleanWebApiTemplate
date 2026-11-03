using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Api.ComponentTests;

/// <summary>
/// Runs the real application in-process against the infrastructure the shared AppHost started (Component mode).
/// Token trust arrives as configuration, through the same keys the AppHost uses in the other modes
/// (AppHostContract.JwtTrust): the application's own validation code runs unchanged.
/// </summary>
public sealed class ComponentAppFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: dev-only behaviour (Swagger, the developer exception page) stays off, as in production.
        builder.UseEnvironment("Testing");

        // Values read eagerly in Program.cs (before Build) see UseSetting; the in-memory source added last also covers
        // values read later. The fixture additionally sets the process environment for eager reads that compete with
        // appsettings.json.
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
            // Safety net: any HttpClient from IHttpClientFactory that targets a non-local host fails fast.
            services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler(() => new BlockExternalHttpHandler()));
        });
    }
}
