using CleanWebApiTemplate.Application.Todo.Create;

namespace CleanWebApiTemplate.Api.Routes.Todo.Create;

/// <summary>Request body for creating a TODO.</summary>
public sealed class CreateTodoRequest
{
    /// <summary>Short task description. Trimmed; unique per owner. 1-255 characters.</summary>
    public required string Title { get; set; }

    /// <summary>Optional details for the task. Up to 1000 characters.</summary>
    public string Description { get; set; } = string.Empty;

    public CreateTodoInput ToInput(string userEmail) => new()
    {
        Title = Title,
        Description = Description,
        CreatedBy = userEmail
    };
}
