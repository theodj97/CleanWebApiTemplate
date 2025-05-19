using CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

namespace CleanWebApiTemplate.Host.Routes.Todo.Get;

public sealed class GetTodoTitlesRequest
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public GetTodoTitleQuery ToQuery() => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties
    };
}
