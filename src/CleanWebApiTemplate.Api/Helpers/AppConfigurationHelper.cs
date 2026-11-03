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
    /// Builds the configuration sources deterministically: files that ship next to the binary, Development-only
    /// user secrets, OpenBao, environment variables and command line. Later sources win. OpenBao is located through
    /// a bootstrap configuration so its address/token can come from the environment or the command line.
    /// </summary>
    private static void ConfigureSources(WebApplicationBuilder builder,
                                         string environment,
                                         string[] args)
    {
        // Bootstrap only to locate OpenBao: its address/token can come from the environment
        // or the command line, which must be read before the OpenBao source is added.
        ConfigurationBuilder bootstrapBuilder = new();
        bootstrapBuilder.SetBasePath(AppContext.BaseDirectory);
        bootstrapBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        bootstrapBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        if (builder.Environment.IsDevelopment())
            bootstrapBuilder.AddUserSecrets<Program>(optional: true);
        bootstrapBuilder.AddEnvironmentVariables();
        bootstrapBuilder.AddCommandLine(args);
        IConfiguration bootstrap = bootstrapBuilder.Build();

        // Rebuild the host sources deterministically instead of stacking duplicates on top of
        // the ones CreateSlimBuilder already added.
        builder.Configuration.Sources.Clear();
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

        // Development-only local secrets; deployed environments use OpenBao or the platform.
        if (builder.Environment.IsDevelopment())
            builder.Configuration.AddUserSecrets<Program>(optional: true);

        builder.Configuration.AddOpenBaoSecrets(bootstrap);
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
