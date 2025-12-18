using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CleanWebApiTemplate.Host;

public static class ConfigureApplication
{
    public static void ConfigureHostApplication(this WebApplication app, string environment)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.ConfigureRoutes(environment);
        app.MapCustomHealthChecks();
    }

    private static void ConfigureRoutes(this WebApplication app, string environment)
    {
        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
        {
            app.MapOpenApi();

            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/openapi/v1.json", "OpenAPI v1");
            });
        }

        // Configure API routes.
        app.MapRoutes();
    }

    private static void MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/api", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api")
        });
    }
}
