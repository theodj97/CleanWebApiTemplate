using Npgsql;

namespace CleanWebApiTemplate.Infrastructure.Data;

/// <summary>
/// Creates new PostgreSQL connections. Connections are opened and disposed per
/// operation ('using' at the call site); Npgsql pools the underlying connections
/// per connection string by default.
/// </summary>
public interface INpgsqlConnectionFactory
{
    public NpgsqlConnection CreateConnection();
}
