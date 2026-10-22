using CleanWebApiTemplate.Infrastructure.Repositories;

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
            Titles = Title?.ToArray(),
            Statuses = Status?.ToArray(),
            CreatedBys = CreatedBy?.ToArray(),
            StartDate = hasDateRange ? DateTime.Parse(StartDate!) : null,
            EndDate = hasDateRange ? DateTime.Parse(EndDate!) : null,
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortProperties = SortProperties
        };
    }
}
