namespace CleanWebApiTemplate.Host.Routes.Todo.Get;

public struct GetTodoTitlesRequest
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
