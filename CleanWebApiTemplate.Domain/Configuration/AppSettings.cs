using System.ComponentModel.DataAnnotations;

namespace CleanWebApiTemplate.Domain.Configuration;

public sealed class AppSettings
{
    [Required]
    public required ConnectionStringsSection ConnectionStrings { get; set; }
    [Required]
    public required string[] CorsAllow { get; set; }
    [Required]
    public required string[] ValidIssuers { get; set; }
    internal const string SECTION_EXTENSION = "Section";
}
