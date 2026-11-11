using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.Update;

/// <summary>Input of the update use case. Omitted fields keep their current value.</summary>
public sealed record UpdateTodoInput
{
    public required string Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public int? Status { get; init; }

    /// <summary>The caller, stored as the last writer when something changes.</summary>
    public required string LastWriter { get; init; }

    /// <summary>Owner filter: the caller's email for non-admins, null for admins.</summary>
    public string? VisibleOwner { get; init; }

    internal bool PatchFields(TodoEntity entityToUpdate)
    {
        bool isUpdated = false;

        if (Title is not null)
        {
            string trimmedTitle = Title.Trim();
            if (entityToUpdate.Title != trimmedTitle)
            {
                entityToUpdate.Title = trimmedTitle;
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
            entityToUpdate.Status = (ETodoStatus)Status.Value;
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
