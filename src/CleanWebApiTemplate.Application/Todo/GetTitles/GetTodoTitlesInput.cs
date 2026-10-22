namespace CleanWebApiTemplate.Application.Todo.GetTitles;

public sealed class GetTodoTitlesInput
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}
