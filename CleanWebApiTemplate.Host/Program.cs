using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Infrastructure;
// using MongoDB.Driver;

namespace CleanWebApiTemplate.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("No environment variable was setted!");

        var appSettings = AppConfigurationHelper.LoadAndRegisterAppSettings(builder, environment);

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
}