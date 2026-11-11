namespace CleanWebApiTemplate.Application.Todo.Delete;

/// <summary>Input of the delete use case.</summary>
public sealed record DeleteTodoInput
{
    public required string Id { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }
}
