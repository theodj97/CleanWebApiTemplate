using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Application.Project;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>The id and name projection of a Project.</summary>
public sealed record ProjectNameResponse : IBaseResponse<ProjectName, ProjectNameResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; init; }

    /// <summary>Task name.</summary>
    public required string Name { get; init; }

    public static ProjectNameResponse ToResponseModel(ProjectName dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
    };
}
