namespace CleanWebApiTemplate.Application.Todo.Delete;

public sealed record DeleteTodoInput
{
    public required string Id { get; set; }
}
