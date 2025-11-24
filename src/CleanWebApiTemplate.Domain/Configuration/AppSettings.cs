namespace CleanWebApiTemplate.Domain.Configuration;

public sealed class AppSettings
{
    public required ConnectionStringsSection ConnectionStrings { get; init; }
    public required string[] CorsAllow { get; init; }
    public required string[] ValidIssuers { get; init; }
    internal const string SECTION_EXTENSION = "Section";
}
