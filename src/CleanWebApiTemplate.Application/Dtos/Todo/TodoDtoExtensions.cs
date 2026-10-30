using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Application.Dtos.Todo;

/// <summary>
/// Entity to DTO mapping. It lives in Application because Application is the layer that
/// exposes DTOs; Domain only deals with entities and the API maps them to responses.
/// </summary>
internal static class TodoDtoExtensions
{
    internal static TodoDto ToDto(this TodoEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>Maps the id/title projection without filling unrelated DTO fields.</summary>
    internal static TodoDto ToTitleDto(this TodoEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new()
        {
            Id = entity.Id,
            Title = entity.Title
        };
    }
}
