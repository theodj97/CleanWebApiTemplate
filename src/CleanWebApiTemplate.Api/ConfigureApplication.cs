using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CleanWebApiTemplate.Api;

public static class ConfigureApplication
{
    public static void ConfigureApiApplication(this WebApplication app, string environment)
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
        app.MapHealthChecks("/health/sqliteDb", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("sqliteDb")
        });
    }
}
