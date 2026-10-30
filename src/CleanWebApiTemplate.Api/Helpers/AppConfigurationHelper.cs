using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Infrastructure.Secrets;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Api.Helpers;

public static class AppConfigurationHelper
{
    public static AppSettings LoadAppSettings(this WebApplicationBuilder builder, string[] args)
    {
        ConfigureSources(builder, builder.Environment.EnvironmentName, args);

        // Manual binding (no reflection-based ConfigurationBinder, Native AOT friendly).
        var appSettings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection
            {
                Postgres = builder.Configuration.GetSection(nameof(AppSettings.ConnectionStrings))
                                              [nameof(ConnectionStringsSection.Postgres)] ?? string.Empty
            },
            CorsAllowedOrigins = GetStringArray(builder.Configuration, Constants.CORS_ALLOWED_ORIGINS),
            CorsAllowCredentials = GetBoolean(builder.Configuration, Constants.CORS_ALLOW_CREDENTIALS),
            OpenApiEnabled = GetBoolean(builder.Configuration, Constants.OPENAPI_ENABLED),
            ValidIssuers = GetStringArray(builder.Configuration, nameof(AppSettings.ValidIssuers)),
            Audience = builder.Configuration[Constants.AUDIENCE] ?? string.Empty
        };

        var validator = new AppSettingsValidator();
        var validationResult = validator.Validate(null, appSettings);

        if (validationResult.Failed) throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings), validationResult.Failures!);

        return appSettings;
    }

    /// <summary>
    /// Reads the files that ship next to the binary and re-states the host sources in the host's own precedence
    /// order, so the last one still wins: files &lt; user secrets &lt; environment variables &lt; command line.
    /// The host already created these sources; re-adding them keeps their relative order intact while pointing
    /// the files at <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    private static void ConfigureSources(WebApplicationBuilder builder,
                                         string environment,
                                         string[] args)
    {
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        builder.Configuration.AddUserSecrets<Program>(optional: true);
        builder.Configuration.AddOpenBaoSecrets(builder.Configuration);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);
    }

    private static string[] GetStringArray(ConfigurationManager configuration, string sectionName) =>
        [.. configuration.GetSection(sectionName)
                         .GetChildren()
                         .Select(x => x.Value)
                         .OfType<string>()];

    private static bool GetBoolean(ConfigurationManager configuration, string key) =>
        bool.TryParse(configuration[key], out var value) && value;
}
