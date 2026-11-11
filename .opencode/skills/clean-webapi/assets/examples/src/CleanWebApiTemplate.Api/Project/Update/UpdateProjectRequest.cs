using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Project.Update;

namespace CleanWebApiTemplate.Api.Project.Update;

/// <summary>Request body for updating a Project. Omitted fields keep their current value.</summary>
public sealed record UpdateProjectRequest
{
    /// <summary>New name. Names stay unique per owner. Up to 255 characters.</summary>
    public string? Name { get; init; }

    /// <summary>New details for the task. Up to 1000 characters.</summary>
    public string? Description { get; init; }

    /// <summary>New status: 1 Draft, 2 Active, 3 Completed, 4 Archived.</summary>
    public int? Status { get; init; }

    public UpdateProjectInput ToInput(string id, OwnerScope owner) => new()
    {
        Id = id,
        Name = Name,
        Description = Description,
        Status = Status,
        LastWriter = owner.Email,
        VisibleOwner = owner.VisibleOwner,
    };
}
