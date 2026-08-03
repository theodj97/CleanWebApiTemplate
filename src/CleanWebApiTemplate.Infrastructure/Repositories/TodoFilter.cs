namespace CleanWebApiTemplate.Infrastructure.Repositories;

/// <summary>
/// Strongly-typed filter for Todo search queries. Every value is sent to the
/// database as a bound parameter; null/empty collections skip the filter.
/// </summary>
public sealed record TodoFilter
{
    public IReadOnlyList<Ulid>? Ids { get; init; }
    public IReadOnlyList<string>? Titles { get; init; }
    public IReadOnlyList<int>? Statuses { get; init; }
    public IReadOnlyList<string>? CreatedBys { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }
}
