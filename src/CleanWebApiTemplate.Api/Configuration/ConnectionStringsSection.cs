
namespace CleanWebApiTemplate.Api.Configuration;

public sealed class ConnectionStringsSection : SectionBase
{
    public required string Postgres { get; init; }
}
