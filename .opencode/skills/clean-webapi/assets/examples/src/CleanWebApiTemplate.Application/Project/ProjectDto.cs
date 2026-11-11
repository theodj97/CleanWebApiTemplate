using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project;

/// <summary>The read model of a Project as the API returns it.</summary>
public sealed record ProjectDto
{
    public required Ulid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required EProjectStatus Status { get; init; }
    public required string CreatedBy { get; init; }
    public required string UpdatedBy { get; init; }
}
