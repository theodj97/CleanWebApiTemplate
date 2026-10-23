namespace CleanWebApiTemplate.Tests.Shared.Database;

/// <summary>Registry of every database the tests can reset, looked up by logical name.</summary>
public sealed class DatabaseResetters
{
    private readonly Dictionary<string, IDatabaseResetter> _resetters = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Names => _resetters.Keys;

    public void Add(IDatabaseResetter resetter) => _resetters.Add(resetter.Name, resetter);

    /// <summary>Resets the named databases, or all of them when <paramref name="names"/> is empty.</summary>
    public async Task ResetAsync(
        IReadOnlyList<string> names,
        IReadOnlyCollection<string> extraTablesToKeep,
        CancellationToken cancellationToken)
    {
        var targets = names.Count == 0 ? _resetters.Values.ToList() : names.Select(Resolve).ToList();

        foreach (var resetter in targets)
        {
            await resetter.ResetAsync(extraTablesToKeep, cancellationToken);
        }
    }

    private IDatabaseResetter Resolve(string name) =>
        _resetters.TryGetValue(name, out var resetter)
            ? resetter
            : throw new InvalidOperationException(
                $"[ResetDatabase(\"{name}\")] does not match any registered database. Registered: {string.Join(", ", _resetters.Keys)}.");
}
