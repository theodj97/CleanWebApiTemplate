namespace CleanWebApiTemplate.Application.Todo.GetById;

public sealed record GetTodoByIdInput
{
    public required string Id { get; set; }
}
