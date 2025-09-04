using CleanWebApiTemplate.Application.Handlers.Todo.Create;

namespace CleanWebApiTemplate.Host.Routes.Todo.Create;

public sealed class CreateTodoRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;

    public CreateTodoCommand ToCommand(string userEmail) => new()
    {
        Title = Title,
        Description = Description,
        CreatedBy = userEmail
    };
}
