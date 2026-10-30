using System.Globalization;
using CleanWebApiTemplate.Application.Todo.Queries;

namespace CleanWebApiTemplate.Application.Todo.Filtered;

public sealed record FilteredTodoInput
{
    public IEnumerable<string>? Ids { get; set; }
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<int>? Status { get; set; } = [];
    public IEnumerable<string>? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;

    internal TodoFilter ToFilter()
    {
        // The date range applies only when both bounds are provided (validated upstream).
        bool hasDateRange = string.IsNullOrEmpty(StartDate) is false && string.IsNullOrEmpty(EndDate) is false;

        return new TodoFilter
        {
            Ids = Ids?.Select(Ulid.Parse).ToArray(),
            Titles = Title?.Where(title => string.IsNullOrWhiteSpace(title) is false)
                          .Select(title => title.Trim())
                          .ToArray(),
            Statuses = Status?.ToArray(),
            CreatedBys = CreatedBy?.ToArray(),
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
