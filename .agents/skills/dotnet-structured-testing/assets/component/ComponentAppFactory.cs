using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using {{AppNamespace}};
using {{SharedNamespace}}.Auth;

namespace {{ComponentTestsNamespace}};

/// <summary>
/// Runs the real application in-process against the infrastructure Aspire started.
/// Only what cannot be reached through configuration is changed here: trust in the test token issuer, background
/// work that interferes with tests, and a guard against outbound calls to real services.
/// </summary>
public sealed class ComponentAppFactory(IReadOnlyDictionary<string, string?> settings, TestTokenIssuer tokenIssuer)
    : WebApplicationFactory<Program> // ADAPT: the application's entry point type
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ADAPT: environment chosen in discovery. The factory default is Development, which can enable dev-only
        // behaviour (seeding, Swagger, exception pages) that production does not have.
        builder.UseEnvironment("Testing");

        // Values read eagerly in Program.cs (before Build) only see UseSetting, and only when Program.cs calls
        // WebApplication.CreateBuilder(args). Values read later (IOptions, IConfiguration at runtime) also see the
        // in-memory source, which is added last and therefore wins over appsettings.*.json.
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings));

        // Development enables these checks; keep them in any environment so DI mistakes surface in tests.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.ConfigureTestServices(services =>
        {
            // Configure runs before the framework's JwtBearer post-configuration (which would otherwise build a
            // remote metadata ConfigurationManager and reject a non-HTTPS authority); PostConfigure runs after every
            // other registration and re-asserts the same values.
            services.ConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);
            services.PostConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);

            // ADAPT: background services that interfere with tests and are not under test.
            // services.RemoveHostedService<OutboxPublisherService>();

            // Safety net: any HttpClient from IHttpClientFactory that targets a non-local host fails fast.
            services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler(() => new BlockExternalHttpHandler()));
        });
    }

    private void TrustTestTokenIssuer(JwtBearerOptions options)
    {
        var configuration = new OpenIdConnectConfiguration { Issuer = tokenIssuer.Issuer };
        configuration.SigningKeys.Add(tokenIssuer.SigningKey);

        // No authority and a static configuration: the handler never downloads metadata.
        options.Authority = null;
        options.MetadataAddress = string.Empty;
        options.RequireHttpsMetadata = false;
        options.Configuration = configuration;
        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);

        var validation = options.TokenValidationParameters;
        validation.ValidateIssuer = true;
        validation.ValidIssuer = tokenIssuer.Issuer;
        validation.ValidIssuers = [tokenIssuer.Issuer];
        validation.ValidateAudience = true;
        validation.ValidAudience = tokenIssuer.Audience;
        validation.ValidAudiences = [tokenIssuer.Audience];
        validation.ValidateIssuerSigningKey = true;
        validation.IssuerSigningKey = tokenIssuer.SigningKey;
        validation.IssuerSigningKeys = [tokenIssuer.SigningKey];
        validation.ValidateLifetime = true;

        // ADAPT: custom validators that need the real identity provider (e.g. Microsoft.Identity.Web's AAD issuer
        // validator) cannot validate self-issued tokens. Role/claim mapping (RoleClaimType, MapInboundClaims) is kept.
        validation.IssuerValidator = null;
        validation.IssuerSigningKeyResolver = null;
    }
}
