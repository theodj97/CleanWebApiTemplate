using StackExchange.Redis;

namespace {{SharedNamespace}}.Database;

/// <summary>
/// Redis is not supported by Respawn. This resetter deletes only the keys whose names start with one of the prefixes
/// listed in <see cref="DatabaseResetConfiguration.RedisKeyPrefixesToReset"/> (SCAN + DEL); it never runs FLUSHDB, so
/// keys outside those prefixes survive and no admin command is needed.
/// </summary>
/// <remarks>
/// Keys are matched literally by prefix: pass the beginning of the key names, including the separator, e.g.
/// <c>"orders:"</c>. Per-test <c>ExcludeTables</c> values are treated as additional key prefixes to keep.
/// Like any destructive resetter, it refuses non-local hosts (see <see cref="DatabaseResetTarget"/>).
/// </remarks>
public sealed class RedisDatabaseResetter : IDatabaseResetter
{
    private const int DeleteBatchSize = 500;

    private readonly IConnectionMultiplexer _redis;
    private readonly IReadOnlyCollection<string> _keyPrefixes;
    private readonly int _database;

    /// <param name="database">
    /// Logical database the application uses (not necessarily 0). It is required on purpose: guessing it would delete
    /// keys in the wrong database and leave the application's data untouched.
    /// </param>
    public RedisDatabaseResetter(
        string name,
        IConnectionMultiplexer redis,
        IReadOnlyCollection<string> keyPrefixes,
        int database)
    {
        if (database < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(database), database, "The Redis database must be >= 0.");
        }

        if (keyPrefixes.Count == 0)
        {
            throw new InvalidOperationException(
                $"'{name}' has no Redis key prefixes to reset. List them in " +
                "DatabaseResetConfiguration.RedisKeyPrefixesToReset: an empty list is never treated as \"delete all\".");
        }

        if (keyPrefixes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"'{name}' contains an empty Redis key prefix.");
        }

        if (keyPrefixes.Any(prefix => prefix.IndexOfAny(['*', '?', '[', ']']) >= 0))
        {
            throw new InvalidOperationException(
                $"'{name}' contains Redis glob characters in a key prefix. Pass literal prefixes (e.g. \"orders:\"), " +
                "not patterns: \"*\" would delete every key.");
        }

        DatabaseResetTarget.EnsureLocal(redis.GetEndPoints(), name);

        Name = name;
        _redis = redis;
        _keyPrefixes = keyPrefixes;
        _database = database;
    }

    public string Name { get; }

    public async Task ResetAsync(IReadOnlyCollection<string> extraTablesToKeep, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(_database);

        foreach (var endpoint in _redis.GetEndPoints())
        {
            var server = _redis.GetServer(endpoint);
            if (server.IsReplica)
            {
                continue;
            }

            foreach (var prefix in _keyPrefixes)
            {
                var batch = new List<RedisKey>(DeleteBatchSize);

                await foreach (var key in server.KeysAsync(_database, $"{prefix}*")
                                   .WithCancellation(cancellationToken))
                {
                    if (IsKept(key, extraTablesToKeep))
                    {
                        continue;
                    }

                    batch.Add(key);
                    if (batch.Count < DeleteBatchSize)
                    {
                        continue;
                    }

                    await database.KeyDeleteAsync(batch.ToArray());
                    batch.Clear();
                }

                if (batch.Count > 0)
                {
                    await database.KeyDeleteAsync(batch.ToArray());
                }
            }
        }
    }

    private static bool IsKept(RedisKey key, IReadOnlyCollection<string> prefixesToKeep)
    {
        var value = (string?)key;
        return value is not null
            && prefixesToKeep.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
    }
}
