namespace CleanWebApiTemplate.Application.Todo.Delete;

public sealed record DeleteTodoInput
{
    public required string Id { get; set; }

    /// <summary>Owner filter: the JWT email for non-admins, null for administrators.</summary>
    public string? CreatedBy { get; init; }
}
