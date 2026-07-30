using CleanWebApiTemplate.Infrastructure.Constants;
using Microsoft.Data.Sqlite;

namespace CleanWebApiTemplate.Infrastructure.Data;

/// <summary>
/// Creates the database schema when it doesn't exist yet (replaces EF Core's
/// EnsureCreated/Migrate design-time workflow with plain DDL).
/// </summary>
public static class SqliteDatabaseInitializer
{
    public static async Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = TodoTable.CreateTableSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
