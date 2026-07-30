using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Configuration;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Host.Helpers;

public static class AppConfigurationHelper
{
    public static (AppSettings appSettings, string environment) LoadAppSettings(this WebApplicationBuilder builder)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("No environment variable was setted!");

        ConfigureSources(builder, environment);

        // Manual binding (no reflection-based ConfigurationBinder, Native AOT friendly).
        var appSettings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection
            {
                Sqlite = builder.Configuration.GetSection(nameof(AppSettings.ConnectionStrings))
                                              [nameof(ConnectionStringsSection.Sqlite)] ?? string.Empty
            },
            CorsAllow = GetStringArray(builder.Configuration, nameof(AppSettings.CorsAllow)),
            ValidIssuers = GetStringArray(builder.Configuration, nameof(AppSettings.ValidIssuers))
        };

        var validator = new AppSettingsValidator();
        var validationResult = validator.Validate(null, appSettings);

        if (validationResult.Failed) throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings), validationResult.Failures!);

        return (appSettings, environment);
    }

    private static void ConfigureSources(WebApplicationBuilder builder,
                                         string environment)
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

    private static string[] GetStringArray(ConfigurationManager configuration, string sectionName) =>
        [.. configuration.GetSection(sectionName)
                         .GetChildren()
                         .Select(x => x.Value)
                         .OfType<string>()];
}
