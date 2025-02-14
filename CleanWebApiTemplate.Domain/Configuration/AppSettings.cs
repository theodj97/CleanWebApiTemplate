namespace CleanWebApiTemplate.Domain.Configuration;

public record AppSettings
{
    public ConnectionStrings? ConnectionStrings { get; set; }
    public string[]? CorwsAllow { get; set; }
    public string[]? ValidIssuers { get; set; }
}
