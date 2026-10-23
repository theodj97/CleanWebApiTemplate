namespace CleanWebApiTemplate.Tests.Shared.Database;

/// <summary>
/// Empties one database between tests. Implementations delete rows and never touch structure (tables, indexes,
/// sequences definitions): the schema is created once per run and must survive every reset.
/// </summary>
public interface IDatabaseResetter
{
    /// <summary>Logical name used by <c>[ResetDatabase("...")]</c>.</summary>
    string Name { get; }

    /// <summary>Deletes the data. <paramref name="extraTablesToKeep"/> are kept for this call only.</summary>
    Task ResetAsync(IReadOnlyCollection<string> extraTablesToKeep, CancellationToken cancellationToken);
}
