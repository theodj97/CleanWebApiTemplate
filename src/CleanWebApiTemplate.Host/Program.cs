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
                                         appSettings.ValidIssuers);

        builder.Services.AddInfrastructureServices(appSettings.ConnectionStrings,
                                                   environment);

        builder.Services.AddApplicationServices();

        var app = builder.Build();
        app.ConfigureHostApplication(environment);
        app.Run();
    }
}