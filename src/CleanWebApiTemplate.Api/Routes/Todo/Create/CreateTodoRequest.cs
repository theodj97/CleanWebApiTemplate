using CleanWebApiTemplate.Application.Todo.Create;

namespace CleanWebApiTemplate.Api.Routes.Todo.Create;

public sealed class CreateTodoRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;

    public CreateTodoInput ToInput(string userEmail) => new()
    {
        Title = Title,
        Description = Description,
        CreatedBy = userEmail
    };
}
