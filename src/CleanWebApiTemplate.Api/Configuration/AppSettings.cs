namespace CleanWebApiTemplate.Api.Configuration;

public sealed class AppSettings
{
    public required ConnectionStringsSection ConnectionStrings { get; init; }
    public required string[] CorsAllowedOrigins { get; init; }
    public required bool CorsAllowCredentials { get; init; }
    public required bool OpenApiEnabled { get; init; }
    public required string[] ValidIssuers { get; init; }
    public required string Audience { get; init; }

    /// <summary>Suffix every section class carries so <see cref="SectionBase"/> can derive its name.</summary>
    internal const string SectionSuffix = "Section";
}
