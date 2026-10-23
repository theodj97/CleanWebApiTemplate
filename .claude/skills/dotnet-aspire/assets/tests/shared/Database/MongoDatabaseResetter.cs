using MongoDB.Bson;
using MongoDB.Driver;

namespace {{SharedNamespace}}.Database;

/// <summary>
/// MongoDB is not supported by Respawn. This resetter empties ONLY the collections listed in
/// <see cref="DatabaseResetConfiguration.MongoCollectionsToReset"/>: enumerating every collection and deleting
/// "everything except the keep list" would also empty collections the app creates on startup but discovery never saw.
/// <c>DeleteMany</c> keeps collections and their indexes (dropping them would also drop indexes the app created at
/// startup).
/// </summary>
/// <remarks>
/// Like the code it deletes, the resetter refuses non-local hosts (see <see cref="DatabaseResetTarget"/>).
/// </remarks>
public sealed class MongoDatabaseResetter : IDatabaseResetter
{
    private readonly IMongoDatabase _database;
    private readonly IReadOnlyCollection<string> _collectionsToReset;

    public MongoDatabaseResetter(
        string name,
        IMongoDatabase database,
        IReadOnlyCollection<string> collectionsToReset)
    {
        if (collectionsToReset.Count == 0)
        {
            throw new InvalidOperationException(
                $"'{name}' has no MongoDB collections to reset. List the collections tests write to in " +
                "DatabaseResetConfiguration.MongoCollectionsToReset: an empty list is never treated as \"delete all\".");
        }

        if (collectionsToReset.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"'{name}' contains an empty MongoDB collection name.");
        }

        var hosts = database.Client.Settings.Servers.Select(server => server.Host).ToList();
        if (hosts.Count == 0 && database.Client.Settings.Server is { } server)
        {
            hosts.Add(server.Host);
        }

        DatabaseResetTarget.EnsureLocal(hosts, name);

        Name = name;
        _database = database;
        _collectionsToReset = collectionsToReset;
    }

    public string Name { get; }

    public async Task ResetAsync(IReadOnlyCollection<string> extraTablesToKeep, CancellationToken cancellationToken)
    {
        foreach (var collection in _collectionsToReset)
        {
            if (collection.StartsWith("system.", StringComparison.Ordinal)
                || extraTablesToKeep.Contains(collection, StringComparer.Ordinal))
            {
                continue;
            }

            await _database.GetCollection<BsonDocument>(collection)
                .DeleteManyAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken);
        }
    }
}
