using CleanWebApiTemplate.Application.Handlers.Todo.Update;

namespace CleanWebApiTemplate.Host.Routes.Todo.Update;

public sealed class UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }

    public UpdateTodoCommand ToCommand(string id, string userEmail) => new()
    {

        Id = id,
        Title = Title,
        Description = Description,
        Status = Status,
        UpdatedBy = userEmail
    };
}
