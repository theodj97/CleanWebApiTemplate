using CleanWebApiTemplate.Application.Todo.GetTitles;

namespace CleanWebApiTemplate.Api.Routes.Todo.Get;

public sealed class GetTodoTitlesRequest
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public GetTodoTitlesInput ToInput() => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties
    };
}
