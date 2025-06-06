using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Configuration;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Host.Helpers;

public static class AppConfigurationHelper
{
    public static AppSettings LoadAndRegisterAppSettings(WebApplicationBuilder builder, string environment)
    {
        ConfigureSources(builder, environment);

        var appSettings = builder.Configuration.Get<AppSettings>()
            ?? throw new InvalidOperationException("AppSetting sconfiguration section is missing or invalid. Ensure configuration sources (appsettings.json, user secrets, or in-memory for tests) are loaded correctly and match the AppSettings structure.");

        var validator = new AppSettingsValidator();
        var validationResult = validator.Validate(null, appSettings);

        if (validationResult.Failed) throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings), validationResult.Failures!);

        return appSettings;
    }

    private static void ConfigureSources(WebApplicationBuilder builder, string environment)
    {
        var basePath = AppContext.BaseDirectory;
        builder.Configuration.SetBasePath(basePath);

        if (File.Exists(Path.Combine(basePath, $"appsettings.{environment}.json")) is false && environment is not Constants.DEV_ENVIRONMNET)
            throw new Exception($"Warning: appsettings.{environment}.json not found.");

        if (environment is Constants.DEV_ENVIRONMNET)
            builder.Configuration.AddUserSecrets<Program>();
        else
            builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: false);
    }
}
