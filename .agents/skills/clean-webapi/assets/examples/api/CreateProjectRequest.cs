using {App}.Application.Abstractions.Identity;
using {App}.Application.Project.Handlers.Create;

namespace {App}.Api.Routes.Project.Create;

/// <summary>Request body for creating a project.</summary>
public sealed class CreateProjectRequest
{
    /// <summary>Project name. Trimmed; unique per owner. 1-120 characters.</summary>
    public required string Name { get; set; }

    /// <summary>Optional details for the project. Up to 1000 characters.</summary>
    public string Description { get; set; } = string.Empty;

    public CreateProjectInput ToInput(OwnerScope owner) => new()
    {
        Name = Name,
        Description = Description,
        CreatedBy = owner.OwnerForCreate
    };
}
