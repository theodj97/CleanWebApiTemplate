using CleanWebApiTemplate.Application.Todo.Filtered;

namespace CleanWebApiTemplate.Api.Routes.Todo.Filter;

public sealed class FilteredTodoRequest
{
    public string[]? Ids { get; set; }
    public string[]? Title { get; set; }
    public int[]? Status { get; set; }

    /// <summary>
    /// Admin-only owner filter. Accepts one or more email addresses; every listed
    /// user is included in the database filter.
    /// </summary>
    public string[]? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public FilteredTodoInput ToInput(string? ownerScope) => new()
    {
        Status = Status,
        Title = Title,
        // Administrators (ownerScope null) may filter by another user. For everyone
        // else the owner is always the authenticated user from the validated JWT.
        CreatedBy = ownerScope is null ? CreatedBy : [ownerScope],
        StartDate = StartDate,
        EndDate = EndDate,
        Ids = Ids,
        SortProperties = SortProperties,
        PageNumber = PageNumber,
        PageSize = PageSize,
    };
}
