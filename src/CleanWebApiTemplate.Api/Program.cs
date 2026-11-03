using CleanWebApiTemplate.Api.Helpers;
using CleanWebApiTemplate.ServiceDefaults;

namespace CleanWebApiTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // CreateSlimBuilder leaves out the Kestrel HTTPS wiring: without it an https:// address stops the host
        // at startup. The certificate and the addresses stay configuration (launch profile or environment).
        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.AddServiceDefaults();

        var appSettings = builder.LoadAppSettings(args);

        builder.Services.AddApiServices(builder.Configuration, appSettings);

        var app = builder.Build();
        app.ConfigureApiApplication(appSettings);
        app.MapDefaultEndpoints();
        app.Run();
    }
}
