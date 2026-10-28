using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.Api.Routes.Todo.Update;

public sealed class UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }

    public UpdateTodoInput ToInput(string id, string userEmail, string? ownerScope) => new()
    {
        Id = id,
        Title = Title,
        Description = Description,
        Status = Status,
        UpdatedBy = userEmail,
        CreatedBy = ownerScope
    };
}
