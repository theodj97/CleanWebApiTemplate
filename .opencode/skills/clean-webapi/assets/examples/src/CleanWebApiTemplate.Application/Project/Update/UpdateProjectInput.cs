using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.Update;

/// <summary>Input of the update use case. Omitted fields keep their current value.</summary>
public sealed record UpdateProjectInput
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Status { get; init; }

    /// <summary>The caller, stored as the last writer when something changes.</summary>
    public required string LastWriter { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }

    internal bool PatchFields(ProjectEntity entityToUpdate)
    {
        bool isUpdated = false;

        if (Name is not null)
        {
            string trimmedName = Name.Trim();
            if (entityToUpdate.Name != trimmedName)
            {
                entityToUpdate.Name = trimmedName;
                isUpdated = true;
            }
        }

        if (Description is not null)
        {
            entityToUpdate.Description = Description;
            isUpdated = true;
        }

        if (Status is not null)
        {
            entityToUpdate.Status = (EProjectStatus)Status.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            entityToUpdate.UpdatedAt = DateTime.UtcNow;
            entityToUpdate.UpdatedBy = LastWriter;
        }

        return isUpdated;
    }
}
