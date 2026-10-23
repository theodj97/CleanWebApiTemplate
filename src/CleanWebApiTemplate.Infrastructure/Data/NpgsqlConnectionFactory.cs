using Npgsql;

namespace CleanWebApiTemplate.Infrastructure.Data;

public sealed class NpgsqlConnectionFactory(string connectionString) : INpgsqlConnectionFactory
{
    private readonly string connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("The PostgreSQL connection string can't be null or empty.", nameof(connectionString))
        : connectionString;

    public NpgsqlConnection CreateConnection() => new(connectionString);
}
