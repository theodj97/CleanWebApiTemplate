using CleanWebApiTemplate.Api.Helpers;
using CleanWebApiTemplate.ServiceDefaults;

namespace CleanWebApiTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.AddServiceDefaults();

        var (appSettings, environment) = builder.LoadAppSettings();

        builder.Services.AddApiServices(builder.Configuration, appSettings, environment);

        var app = builder.Build();
        app.ConfigureApiApplication(environment);
        app.MapDefaultEndpoints();
        app.Run();
    }
}
