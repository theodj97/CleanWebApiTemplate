using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Todo.Create;

namespace CleanWebApiTemplate.Api.Todo.Create;

/// <summary>Request body for creating a Todo.</summary>
public sealed record CreateTodoRequest
{
    /// <summary>Short task description. Trimmed; unique per owner. 1-255 characters.</summary>
    public required string Title { get; init; }

    /// <summary>Optional details for the task. Up to 1000 characters.</summary>
    public string? Description { get; init; }

    public CreateTodoInput ToInput(OwnerScope owner) => new()
    {
        Title = Title,
        Description = Description ?? string.Empty,
        Owner = owner.OwnerForCreate,
    };
}
