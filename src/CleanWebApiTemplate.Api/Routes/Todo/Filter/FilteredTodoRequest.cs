using CleanWebApiTemplate.Application.Todo.Filtered;

namespace CleanWebApiTemplate.Api.Routes.Todo.Filter;

/// <summary>Filter, paging and sorting options for the TODO list.</summary>
public sealed class FilteredTodoRequest
{
    /// <summary>TODO ids to include.</summary>
    public string[]? Ids { get; set; }

    /// <summary>Title fragments to match (contains).</summary>
    public string[]? Title { get; set; }

    /// <summary>Statuses to include: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public int[]? Status { get; set; }

    /// <summary>
    /// Admin-only owner filter. Accepts one or more email addresses; every listed
    /// user is included in the database filter.
    /// </summary>
    public string[]? CreatedBy { get; set; }

    /// <summary>Inclusive lower bound for CreatedAt.</summary>
    public string? StartDate { get; set; }

    /// <summary>Inclusive upper bound for CreatedAt.</summary>
    public string? EndDate { get; set; }

    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; set; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; set; }

    /// <summary>
    /// Sort keys and directions, applied in order. Supported keys: id, title, description,
    /// createdAt, updatedAt, status, createdBy, updatedBy.
    /// </summary>
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
