using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CleanWebApiTemplate.Api;

public static class ConfigureApplication
{
    public static void ConfigureApiApplication(this WebApplication app, AppSettings appSettings)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseHttpsRedirection();

        // After routing and before authorization; the policy allows no origin when none is configured.
        app.UseCors(Constants.DEFAULT_CORS_POLICY_NAME);

        app.UseAuthentication();
        app.UseAuthorization();

        app.ConfigureRoutes(appSettings.OpenApiEnabled);
        app.MapCustomHealthChecks();
    }

    private static void ConfigureRoutes(this WebApplication app, bool openApiEnabled)
    {
        // The OpenAPI document feeds the local swagger sidecar; the API does not serve a UI.
        if (openApiEnabled)
            app.MapOpenApi();

        // Configure API routes.
        app.MapRoutes();
    }

    private static void MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/api", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api")
        });
        app.MapHealthChecks("/health/postgresDb", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("postgresDb")
        });
    }
}
