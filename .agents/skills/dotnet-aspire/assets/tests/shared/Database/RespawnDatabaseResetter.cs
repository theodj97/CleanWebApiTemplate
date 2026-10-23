using System.Collections.Concurrent;
using System.Data.Common;
using Respawn;
using Respawn.Graph;

namespace {{SharedNamespace}}.Database;

/// <summary>
/// Respawn-based reset for SQL Server, PostgreSQL, MySQL/MariaDB and Oracle.
/// Respawn reads the table graph (foreign keys, delete order) when a <see cref="Respawner"/> is created, so the
/// resetter must be created AFTER the schema exists, once per run, and kept in the fixture.
/// </summary>
/// <example>
/// ADAPT the options per engine (see references/database.md):
/// <code>
/// // PostgreSQL: open connection, explicit schemas, sequences restarted.
/// await RespawnDatabaseResetter.CreateAsync(Names.Orders, () => new NpgsqlConnection(cs), new RespawnerOptions
/// {
///     DbAdapter = DbAdapter.Postgres,
///     SchemasToInclude = ["public"],
///     TablesToIgnore = DatabaseResetConfiguration.TablesToIgnore(Names.Orders),
///     WithReseed = true,
/// }, ct);
///
/// // SQL Server: identities reseeded, system-versioned tables handled.
/// new RespawnerOptions { DbAdapter = DbAdapter.SqlServer, TablesToIgnore = ..., WithReseed = true, CheckTemporalTables = true }
/// </code>
/// </example>
public sealed class RespawnDatabaseResetter : IDatabaseResetter
{
    private readonly Func<DbConnection> _createConnection;
    private readonly RespawnerOptions _options;
    private readonly ConcurrentDictionary<string, Respawner> _variants = new(StringComparer.OrdinalIgnoreCase);
    private Respawner _default = null!;

    private RespawnDatabaseResetter(string name, Func<DbConnection> createConnection, RespawnerOptions options)
    {
        Name = name;
        _createConnection = createConnection;
        _options = options;
    }

    public string Name { get; }

    public static async Task<RespawnDatabaseResetter> CreateAsync(
        string name,
        Func<DbConnection> createConnection,
        RespawnerOptions options,
        CancellationToken cancellationToken)
    {
        var resetter = new RespawnDatabaseResetter(name, createConnection, options);

        await using var connection = createConnection();
        await connection.OpenAsync(cancellationToken);
        resetter._default = await Respawner.CreateAsync(connection, options);

        return resetter;
    }

    public async Task ResetAsync(IReadOnlyCollection<string> extraTablesToKeep, CancellationToken cancellationToken)
    {
        await using var connection = _createConnection();
        await connection.OpenAsync(cancellationToken);

        var respawner = extraTablesToKeep.Count == 0
            ? _default
            : await GetVariantAsync(connection, extraTablesToKeep);

        await respawner.ResetAsync(connection);
    }

    // RespawnerOptions cannot change once the Respawner exists, so a test that keeps extra tables needs its own
    // Respawner. It is created the first time that table set is requested and cached for the rest of the run.
    private async Task<Respawner> GetVariantAsync(DbConnection connection, IReadOnlyCollection<string> extraTablesToKeep)
    {
        var key = string.Join("|", extraTablesToKeep.Order(StringComparer.OrdinalIgnoreCase));
        if (_variants.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var options = new RespawnerOptions
        {
            DbAdapter = _options.DbAdapter,
            SchemasToInclude = _options.SchemasToInclude,
            SchemasToExclude = _options.SchemasToExclude,
            TablesToInclude = _options.TablesToInclude,
            TablesToIgnore = [.. _options.TablesToIgnore, .. extraTablesToKeep.Select(ParseTable)],
            CheckTemporalTables = _options.CheckTemporalTables,
            WithReseed = _options.WithReseed,
            CommandTimeout = _options.CommandTimeout,
            FormatDeleteStatement = _options.FormatDeleteStatement,
        };

        var created = await Respawner.CreateAsync(connection, options);
        _variants[key] = created;
        return created;
    }

    private static Table ParseTable(string table)
    {
        var parts = table.Split('.', 2);
        return parts.Length == 2 ? new Table(parts[0], parts[1]) : new Table(table);
    }
}
