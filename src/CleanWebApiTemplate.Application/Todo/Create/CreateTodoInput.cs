using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Application.Todo.Create;

public sealed record CreateTodoInput
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string CreatedBy { get; init; }

    internal TodoEntity ToEntity() => new()
    {
        Id = Ulid.NewUlid(),
        Title = Title,
        Description = Description,
        CreatedBy = CreatedBy,
        UpdatedBy = CreatedBy
    };
}
