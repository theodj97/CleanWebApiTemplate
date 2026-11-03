using Npgsql;

namespace CleanWebApiTemplate.DbInitializer;

/// <summary>
/// Local-only one-shot job: builds the PostgreSQL database from scratch on every AppHost start.
/// Applies the ordered SQL scripts of local/database (schema) and, in Local mode, local/database/seed.
/// Exits 0 when the schema is ready so the API can start; non-zero leaves the API waiting.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ConnectionStrings__Postgres is not configured.");
            return 1;
        }

        var schemaDirectory = Environment.GetEnvironmentVariable("Schema__Path");
        if (string.IsNullOrWhiteSpace(schemaDirectory) || Directory.Exists(schemaDirectory) is false)
        {
            Console.Error.WriteLine($"Schema__Path is not an existing directory: '{schemaDirectory}'.");
            return 1;
        }

        var seedDirectory = Environment.GetEnvironmentVariable("Seed__Path");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await ApplyScriptsAsync(connection, schemaDirectory, "schema");

            if (string.IsNullOrWhiteSpace(seedDirectory))
                Console.WriteLine("No seed directory configured; skipping seed.");
            else if (Directory.Exists(seedDirectory))
                await ApplyScriptsAsync(connection, seedDirectory, "seed");
            else
                Console.WriteLine($"Seed directory '{seedDirectory}' does not exist; skipping seed.");

            await using var command = connection.CreateCommand();
            command.CommandText = """SELECT COUNT(1) FROM "Todo";""";
            var rows = (long)(await command.ExecuteScalarAsync())!;

            Console.WriteLine($"Database ready: table 'Todo' exists with {rows} row(s).");
            return 0;
        }
        catch (Exception exception) when (exception is NpgsqlException or IOException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"Database initialization failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task ApplyScriptsAsync(NpgsqlConnection connection, string directory, string kind)
    {
        var scripts = Directory.GetFiles(directory, "*.sql")
                               .Order(StringComparer.Ordinal)
                               .ToArray();

        if (scripts.Length is 0)
        {
            if (kind is "schema")
                throw new InvalidOperationException($"No *.sql scripts found in '{directory}'.");

            Console.WriteLine($"No {kind} scripts found in '{directory}'.");
            return;
        }

        foreach (var script in scripts)
        {
            await using var transaction = await connection.BeginTransactionAsync();

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = (NpgsqlTransaction)transaction;
                command.CommandText = await File.ReadAllTextAsync(script);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            Console.WriteLine($"Applied {kind} {Path.GetFileName(script)}.");
        }
    }
}
