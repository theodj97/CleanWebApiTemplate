namespace CleanWebApiTemplate.Application.Todo.GetTitles;

/// <summary>Input of the titles projection use case.</summary>
public sealed record GetTodoTitlesInput
{
    public byte? PageNumber { get; init; }
    public byte? PageSize { get; init; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }
}
