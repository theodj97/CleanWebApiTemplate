using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project;

/// <summary>
/// Entity to DTO mapping. It lives in Application because Application is the layer that exposes DTOs;
/// Domain only deals with entities and the API maps them to responses.
/// </summary>
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
            UpdatedBy = entity.UpdatedBy,
        };
    }
}
