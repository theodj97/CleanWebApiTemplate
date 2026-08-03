using Microsoft.Data.Sqlite;

namespace CleanWebApiTemplate.Infrastructure.Data;

/// <summary>
/// Creates new SQLite connections. Connections are opened and disposed per
/// operation ('using' at the call site); Microsoft.Data.Sqlite pools the
/// underlying native connections per connection string by default.
/// </summary>
public interface ISqliteConnectionFactory
{
    SqliteConnection CreateConnection();
}
