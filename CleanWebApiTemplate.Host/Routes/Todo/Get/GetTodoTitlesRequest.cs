namespace CleanWebApiTemplate.Host.Routes.Todo.Get;

public struct GetTodoTitlesRequest
{
    public byte PageNumber { get; set; }
    public byte PageSize { get; set; }
}
