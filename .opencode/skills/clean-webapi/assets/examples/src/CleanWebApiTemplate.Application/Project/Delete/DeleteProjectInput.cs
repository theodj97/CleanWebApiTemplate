namespace CleanWebApiTemplate.Application.Project.Delete;

/// <summary>Input of the delete use case.</summary>
public sealed record DeleteProjectInput
{
    public required string Id { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }
}
