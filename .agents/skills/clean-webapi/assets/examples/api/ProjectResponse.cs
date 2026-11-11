using {App}.Application.Dtos.Project;

namespace {App}.Api.Models.Responses.Project;

/// <summary>A project returned by the API.</summary>
public sealed record ProjectResponse : IBaseResponse<ProjectDto?, ProjectResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; set; }

    /// <summary>Project name.</summary>
    public required string Name { get; set; }

    /// <summary>Project details.</summary>
    public required string Description { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>Status: 1 Draft, 2 Active, 3 Completed, 4 Archived.</summary>
    public required int Status { get; set; }

    /// <summary>Owner email.</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Email of the last writer.</summary>
    public required string UpdatedBy { get; set; }

    public static ProjectResponse? ToResponseModel(ProjectDto? dto)
    {
        if (dto is null) return null;

        return new()
        {
            Id = (Ulid)dto.Id!,
            Name = dto.Name!,
            Description = dto.Description ?? string.Empty,
            CreatedAt = (DateTime)dto.CreatedAt!,
            Status = (int)dto.Status!,
            CreatedBy = dto.CreatedBy!,
            UpdatedBy = dto.UpdatedBy!
        };
    }
}
