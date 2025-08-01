using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Infrastructure;

namespace CleanWebApiTemplate.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var (appSettings, environment) = builder.LoadWebApiSettings();

        builder.Services.AddHostServices(builder.Configuration,
                                         environment,
                                         appSettings.CorsAllow,
                                         appSettings.ValidIssuers,
                                         appSettings.ConnectionStrings);

        builder.Services.AddInfrastructureServices(appSettings.ConnectionStrings);

        builder.Services.AddApplicationServices();

        builder.AddFluentValidationEndpointFilter();

        var app = builder.Build();
        app.ConfigureHostApplication(environment);
        app.Run();
    }
}