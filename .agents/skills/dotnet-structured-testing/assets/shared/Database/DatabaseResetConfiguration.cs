using Respawn.Graph;

namespace {{SharedNamespace}}.Database;

/// <summary>
/// The single place that says what a reset touches and what survives it. ADAPT from discovery section 3.
/// Respawn-backed databases use <see cref="TablesToIgnore"/>; the engines Respawn does not support use an explicit
/// allowlist: <see cref="MongoCollectionsToReset"/> and <see cref="RedisKeyPrefixesToReset"/>. An empty allowlist is
/// rejected by the resetter, never treated as "delete everything".
/// </summary>
public static class DatabaseResetConfiguration
{
    /// <summary>Logical names used by <c>[ResetDatabase("...")]</c>.</summary>
    public static class Names
    {
        public const string Orders = "OrdersDb";
        // public const string Catalog = "CatalogDb";
    }

    /// <summary>
    /// Relational tables Respawn must not empty. Keep here only tables nobody writes during tests: migration history
    /// and reference data seeded with the schema.
    /// A table that tests also write to (for example users that must exist) is NOT listed: it is reset like any other
    /// table and its required rows are re-inserted after each reset (TestServerFixtureBase.SeedPersistentDataAsync).
    /// </summary>
    public static Table[] TablesToIgnore(string databaseName) => databaseName switch
    {
        Names.Orders =>
        [
            // Migration history: emptying it would make the next migration run try to recreate the schema.
            new Table("public", "__EFMigrationsHistory"),
            // Reference data inserted by HasData / seed scripts and only read by the app.
            new Table("public", "Countries"),
        ],
        _ => [],
    };

    /// <summary>
    /// MongoDB collections the tests write to; ONLY these are emptied. Never list a collection that must survive.
    /// ADAPT: names from discovery (entity mappings, repositories, <c>GetCollection&lt;T&gt;("...")</c>).
    /// </summary>
    public static string[] MongoCollectionsToReset(string databaseName) => databaseName switch
    {
        // Names.Catalog => ["products", "categories"],
        _ => [],
    };

    /// <summary>
    /// Redis key prefixes owned by the application; ONLY keys starting with one of them are deleted. Include the
    /// separator (<c>"orders:"</c>, not <c>"orders"</c>) so a prefix cannot match an unrelated key by accident.
    /// ADAPT: key builders and cache instance names from discovery.
    /// </summary>
    public static string[] RedisKeyPrefixesToReset(string databaseName) => databaseName switch
    {
        // Names.Catalog => ["catalog:", "orders:"],
        _ => [],
    };
}
