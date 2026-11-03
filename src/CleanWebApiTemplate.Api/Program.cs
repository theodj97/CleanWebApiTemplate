using CleanWebApiTemplate.Api.Helpers;
using CleanWebApiTemplate.ServiceDefaults;

namespace CleanWebApiTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // A missing validator registration must fail at startup, not silently disable validation.
        // The host validates the whole graph, including every handler's ValidatorSet.
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

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
