using {App}.Domain.Models.Entities;

namespace {App}.Application.Dtos.Project;

/// <summary>Entity to DTO mapping. It lives in Application because Application exposes DTOs.</summary>
internal static class ProjectDtoExtensions
{
    internal static ProjectDto ToDto(this ProjectEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    // A partial read does not use a partially filled ProjectDto. Add a projection type in
    // Application/{Feature}/Projections (for example ProjectTitle) and return it from the port.
}
