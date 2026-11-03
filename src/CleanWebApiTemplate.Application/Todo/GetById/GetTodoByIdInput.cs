namespace CleanWebApiTemplate.Application.Todo.GetById;

public sealed record GetTodoByIdInput
{
    public required string Id { get; set; }

    /// <summary>Owner filter: the JWT email for non-admins, null for administrators.</summary>
    public string? CreatedBy { get; init; }
}
