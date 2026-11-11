using System.Globalization;
using {App}.Application.Project.Handlers.Queries;

namespace {App}.Application.Project.Handlers.Filtered;

public sealed record FilteredProjectInput
{
    public IEnumerable<string>? Ids { get; set; }
    public IEnumerable<string>? Names { get; set; }
    public IEnumerable<int>? Statuses { get; set; } = [];
    public IEnumerable<string>? CreatedBys { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

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
            CreatedBys = CreatedBys?.ToArray(),
            StartDate = hasDateRange ? ParseDate(StartDate!) : null,
            EndDate = hasDateRange ? ParseDate(EndDate!) : null,
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortProperties = SortProperties
        };
    }

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
