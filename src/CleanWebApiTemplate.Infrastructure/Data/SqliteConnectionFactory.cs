using Microsoft.Data.Sqlite;

namespace CleanWebApiTemplate.Infrastructure.Data;

public sealed class SqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
{
    private readonly string connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("The SQLite connection string can't be null or empty.", nameof(connectionString))
        : connectionString;

    public SqliteConnection CreateConnection() => new(connectionString);
}
