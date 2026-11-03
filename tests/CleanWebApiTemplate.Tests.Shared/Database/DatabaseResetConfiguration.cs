using Respawn.Graph;

namespace CleanWebApiTemplate.Tests.Shared.Database;

/// <summary>
/// The single place that says what a reset touches and what survives it.
/// The schema is created from <c>local/database/*.sql</c>; there is no migration-history table and no reference
/// data seeded with the schema (the synthetic seed runs only in Local mode), so nothing survives a reset.
/// </summary>
public static class DatabaseResetConfiguration
{
    /// <summary>Logical names used by <c>[ResetDatabase("...")]</c>.</summary>
    public static class Names
    {
        public const string Todo = "TodoDb";
    }

    /// <summary>
    /// Relational tables Respawn must not empty. Keep here only tables nobody writes during tests: migration
    /// history and reference data seeded with the schema. A table that tests also write to is reset like any other.
    /// </summary>
    public static Table[] TablesToIgnore(string databaseName) => databaseName switch
    {
        Names.Todo => [],
        _ => [],
    };
}
