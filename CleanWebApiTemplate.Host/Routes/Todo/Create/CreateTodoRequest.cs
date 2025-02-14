namespace CleanWebApiTemplate.Host.Routes.Todo.Create;

public record CreateTodoRequest
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
}
