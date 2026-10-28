namespace CleanWebApiTemplate.Application.Todo.GetTitles;

public sealed class GetTodoTitlesInput
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;

    /// <summary>Owner filter: the JWT email for non-admins, null for administrators.</summary>
    public string? CreatedBy { get; set; }
}
