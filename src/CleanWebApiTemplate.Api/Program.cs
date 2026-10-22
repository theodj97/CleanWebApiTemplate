using CleanWebApiTemplate.Api.Helpers;
using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Infrastructure;

namespace CleanWebApiTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var (appSettings, environment) = builder.LoadAppSettings();

        builder.Services.AddApiServices(builder.Configuration,
                                         environment,
                                         appSettings.CorsAllow,
                                         appSettings.ValidIssuers,
                                         appSettings.ConnectionStrings);
        builder.Services.AddInfrastructureServices(appSettings.ConnectionStrings);
        builder.Services.AddApplicationServices();

        var app = builder.Build();
        app.ConfigureApiApplication(environment);
        app.Run();
    }
}
