using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Routes;
using CleanWebApiTemplate.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace CleanWebApiTemplate.Host;

public class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("No environment variable was setted!");

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, $"appsettings.{environment}.json")) is false
            && environment is not Constants.DEV_ENVIRONMNET && environment is not Constants.TEST_ENVIRONMNET)
            throw new Exception($"Warning: appsettings.{environment}.json not found.");

        builder.Configuration.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        if (environment is Constants.DEV_ENVIRONMNET)
            builder.Configuration.AddUserSecrets<Program>();
        else if (environment is not Constants.TEST_ENVIRONMNET)
            builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

        var appSettings = ConfigureAppSettings(builder, environment);

        // Adding DI Services.
        builder.Services.AddHostServices(builder.Configuration,
                                         environment,
                                         appSettings.CorsAllow,
                                         appSettings.ValidIssuers,
                                         appSettings.ConnectionStrings!.SqlServer,
                                         appSettings.ConnectionStrings!.MongoDb);
        builder.Services.AddInfrastructureServices(appSettings.ConnectionStrings.SqlServer,
                                                   appSettings.ConnectionStrings.MongoDb);
        builder.Services.AddApplicationServices();

        builder.AddFluentValidationEndpointFilter();

        var app = builder.Build();

        app.ConfigureHostApplication();
        ConfigureRoutes(app, environment);

        app.Run();
    }

    private static void ConfigureRoutes(WebApplication app, string environment)
    {
        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
        {
            app.MapOpenApi();

            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/openapi/v1.json", "OpenAPI v1");
            });
        }

        app.MapHealthChecks("/health/api", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api")
        });

        app.MapHealthChecks("/health/sqlServerDb", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("sqlServerDb")
        });

        app.MapHealthChecks("/health/mongoDb", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("mongoDb")
        });

        // Configure API routes.
        app.MapRoutes();
    }

    private static AppSettings ConfigureAppSettings(WebApplicationBuilder builder, string environment)
    {
        AppSettings appSettings = new();
        builder.Configuration.Bind(appSettings);

        if (environment is not Constants.DEV_ENVIRONMNET && environment is not Constants.TEST_ENVIRONMNET)
            appSettings.ConnectionStrings = new()
            {
                SqlServer = builder.Configuration[Constants.SQLSERVER_CNNSTRING]
                ?? throw new Exception("No sql server cnnstring recieved in envs!"),
                MongoDb = MongoUrl.Create(builder.Configuration[Constants.MONGODB_CNNSTRING])
                ?? throw new Exception("No mongodb cnnstring recieved in envs!")
            };
        if (appSettings.ConnectionStrings is null
            || string.IsNullOrEmpty(appSettings.ConnectionStrings.SqlServer)
            || appSettings.ConnectionStrings.MongoDb is null)
            throw new Exception("ConnectionString should have a value!");

        return appSettings;
    }
}