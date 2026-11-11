using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Application.Project;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>A Project item returned by the API.</summary>
public sealed record ProjectResponse : IBaseResponse<ProjectDto, ProjectResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; init; }

    /// <summary>Task name.</summary>
    public required string Name { get; init; }

    /// <summary>Task details.</summary>
    public required string Description { get; init; }

    /// <summary>Creation timestamp (UTC).</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Last update timestamp (UTC).</summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>Status: 1 Draft, 2 Active, 3 Completed, 4 Archived.</summary>
    public required int Status { get; init; }

    /// <summary>Owner email.</summary>
    public required string CreatedBy { get; init; }

    /// <summary>Email of the last writer.</summary>
    public required string UpdatedBy { get; init; }

    public static ProjectResponse ToResponseModel(ProjectDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        Status = (int)dto.Status,
        CreatedBy = dto.CreatedBy,
        UpdatedBy = dto.UpdatedBy,
    };
}
