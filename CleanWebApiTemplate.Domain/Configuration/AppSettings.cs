namespace CleanWebApiTemplate.Domain.Configuration;

public record AppSettings
{
    public ConnectionStrings? ConnectionStrings { get; set; }
    public string[]? CorsAllow { get; set; }
    public string[]? ValidIssuers { get; set; }
}
