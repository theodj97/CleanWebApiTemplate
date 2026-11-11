using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.Create;

/// <summary>Input of the create use case. <see cref="Owner"/> is the caller, never a request body value.</summary>
public sealed record CreateTodoInput
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string Owner { get; init; }

    internal TodoEntity ToEntity() => new()
    {
        Id = Ulid.NewUlid(),
        Title = Title.Trim(),
        Description = Description,
        Status = ETodoStatus.Pending,
        CreatedBy = Owner,
        UpdatedBy = Owner,
    };
}
