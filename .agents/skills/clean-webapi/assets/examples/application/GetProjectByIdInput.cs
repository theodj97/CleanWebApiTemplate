namespace {App}.Application.Project.Handlers.GetById;

public sealed record GetProjectByIdInput
{
    public required string Id { get; set; }

    /// <summary>Owner filter: the JWT email for non-admins, null for administrators.</summary>
    public string? CreatedBy { get; init; }
}
