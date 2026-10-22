using CleanWebApiTemplate.Application.Todo.Filtered;

namespace CleanWebApiTemplate.Api.Routes.Todo.Filter;

public sealed class FilteredTodoRequest
{
    public string[]? Ids { get; set; }
    public string[]? Title { get; set; }
    public int[]? Status { get; set; }
    public string[]? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public FilteredTodoInput ToInput() => new()
    {
        Status = Status,
        Title = Title,
        CreatedBy = CreatedBy,
        StartDate = StartDate,
        EndDate = EndDate,
        Ids = Ids,
        SortProperties = SortProperties,
        PageNumber = PageNumber,
        PageSize = PageSize,
    };
}
