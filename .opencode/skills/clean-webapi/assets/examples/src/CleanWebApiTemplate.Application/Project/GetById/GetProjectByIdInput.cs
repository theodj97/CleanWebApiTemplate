namespace CleanWebApiTemplate.Application.Project.GetById;

/// <summary>Input of the get-by-id use case.</summary>
public sealed record GetProjectByIdInput
{
    public required string Id { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }
}
