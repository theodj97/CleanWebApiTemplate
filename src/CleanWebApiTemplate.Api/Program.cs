using CleanWebApiTemplate.Api.Common.Composition;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Helpers;
using CleanWebApiTemplate.ServiceDefaults;

namespace CleanWebApiTemplate.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // A missing registration must fail at startup, not on the first request.
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        // CreateSlimBuilder leaves out the Kestrel HTTPS wiring: without it an https:// address stops the host
        // at startup. The certificate and the addresses stay configuration (launch profile or environment).
        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.AddServiceDefaults();

        AppSettings appSettings = builder.LoadAppSettings(args);

        builder.Services.AddApiServices(builder.Configuration, appSettings);

        var app = builder.Build();

        // Builds every use case once: a handler without validators or a missing registration stops the host here.
        app.Services.ValidateUseCases();

        app.ConfigureApiApplication(appSettings);
        app.MapDefaultEndpoints();
        app.Run();
    }
}
