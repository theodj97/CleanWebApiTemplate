using System.Globalization;

namespace CleanWebApiTemplate.Application.Project.Filter;

/// <summary>Input of the filter use case.</summary>
public sealed record FilteredProjectInput
{
    public IEnumerable<string>? Ids { get; init; }
    public IEnumerable<string>? Names { get; init; }
    public IEnumerable<int>? Statuses { get; init; }

    /// <summary>Owner filter: admins keep the requested owners, everyone else is pinned to their own email.</summary>
    public IEnumerable<string>? Owners { get; init; }

    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public byte? PageNumber { get; init; }
    public byte? PageSize { get; init; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    internal ProjectFilter ToFilter()
    {
        // The date range applies only when both bounds are provided (validated upstream).
        bool hasDateRange = string.IsNullOrEmpty(StartDate) is false && string.IsNullOrEmpty(EndDate) is false;

        return new ProjectFilter
        {
            Ids = Ids?.Select(id => Ulid.Parse(id, CultureInfo.InvariantCulture)).ToArray(),
            Names = Names?.Where(name => string.IsNullOrWhiteSpace(name) is false)
                           .Select(name => name.Trim())
                           .ToArray(),
            Statuses = Statuses?.ToArray(),
            Owners = Owners?.ToArray(),
            StartDate = hasDateRange ? ParseDate(StartDate!) : null,
            EndDate = hasDateRange ? ParseDate(EndDate!) : null,
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortProperties = SortProperties,
        };
    }

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
