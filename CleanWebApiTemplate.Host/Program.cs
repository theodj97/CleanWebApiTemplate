using System.ComponentModel.DataAnnotations;
using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure;
// using MongoDB.Driver;

namespace CleanWebApiTemplate.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("No environment variable was setted!");

        var appSettings = ConfigureAppSettings(builder, environment);

        builder.Services.AddHostServices(builder.Configuration, environment, appSettings.CorsAllow, appSettings.ValidIssuers, appSettings.ConnectionStrings);
        builder.Services.AddInfrastructureServices(appSettings.ConnectionStrings);
        builder.Services.AddApplicationServices();

        builder.AddFluentValidationEndpointFilter();

        var app = builder.Build();

        app.ConfigureHostApplication();
        app.ConfigureRoutes(environment);
        app.MapCustomHealthChecks();

        app.Run();
    }

    private static AppSettings ConfigureAppSettings(WebApplicationBuilder builder, string environment)
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, $"appsettings.{environment}.json")) is false
            && environment is not Constants.DEV_ENVIRONMNET
            && environment is not Constants.TEST_ENVIRONMNET)
            throw new Exception($"Warning: appsettings.{environment}.json not found.");

        if (environment is not Constants.TEST_ENVIRONMNET)
            builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        if (environment is Constants.DEV_ENVIRONMNET)
            builder.Configuration.AddUserSecrets<Program>();
        else if (environment is not Constants.TEST_ENVIRONMNET)
            builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

        var appSettings = builder.Configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("AppSettings configuration section is missing or invalid.");

        Validator.ValidateObject(appSettings, new ValidationContext(appSettings), validateAllProperties: true);

        builder.Services.AddOptions<ConnectionStringsSection>(ConnectionStringsSection.SectionName)
            .Bind(builder.Configuration.GetSection(ConnectionStringsSection.SectionName))
            .ValidateDataAnnotations();

        return appSettings;
    }
}