
namespace CleanWebApiTemplate.Domain.Configuration;

public sealed class ConnectionStringsSection : SectionBase
{
#if (IsSQLite)
    public required string Sqlite { get; init; }
#endif
}
