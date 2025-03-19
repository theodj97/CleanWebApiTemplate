namespace CleanWebApiTemplate.Host.Routes.Todo.Create;

public struct CreateTodoRequest
{
    public required string Title { get; set; }
    public string Description { get; set; }
}
