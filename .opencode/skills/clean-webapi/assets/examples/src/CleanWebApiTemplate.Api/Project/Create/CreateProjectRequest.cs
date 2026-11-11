using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Project.Create;

namespace CleanWebApiTemplate.Api.Project.Create;

/// <summary>Request body for creating a Project.</summary>
public sealed record CreateProjectRequest
{
    /// <summary>Short task description. Trimmed; unique per owner. 1-255 characters.</summary>
    public required string Name { get; init; }

    /// <summary>Optional details for the task. Up to 1000 characters.</summary>
    public string? Description { get; init; }

    public CreateProjectInput ToInput(OwnerScope owner) => new()
    {
        Name = Name,
        Description = Description ?? string.Empty,
        Owner = owner.OwnerForCreate,
    };
}
