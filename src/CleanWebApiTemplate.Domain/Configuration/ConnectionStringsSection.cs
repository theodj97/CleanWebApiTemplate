
namespace CleanWebApiTemplate.Domain.Configuration;

public sealed class ConnectionStringsSection : SectionBase
{
    public required string MariaDb { get; init; }
}
