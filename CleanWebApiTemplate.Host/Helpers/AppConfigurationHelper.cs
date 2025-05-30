using CleanWebApiTemplate.Domain.Configuration;

namespace CleanWebApiTemplate.Host.Helpers;

public static class AppConfigurationHelper
{
    public static AppSettings LoadAndRegisterAppSettings(WebApplicationBuilder builder, string environment)
    {
        ConfigureSources(builder, environment);

        var appSettings = builder.Configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("AppSetting sconfiguration section is missing or invalid. " + "Ensure configuration sources (appsettings.json, user secrets, or in-memory for tests) are loaded correctly " + "and match the AppSettings structure.");

        return appSettings;
    }

    private static void ConfigureSources(WebApplicationBuilder builder, string environment)
    {
        var basePath = AppContext.BaseDirectory;

        if (File.Exists(Path.Combine(basePath, $"appsettings.{environment}.json")) is false
            && environment is not Constants.DEV_ENVIRONMNET
            && environment is not Constants.TEST_ENVIRONMNET)
            throw new Exception($"Warning: appsettings.{environment}.json not found.");

        if (environment is not Constants.TEST_ENVIRONMNET)
        {
            builder.Configuration.SetBasePath(basePath)
                                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            if (environment is Constants.DEV_ENVIRONMNET)
                builder.Configuration.AddUserSecrets<Program>();
            else
                builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
        }
    }
}
