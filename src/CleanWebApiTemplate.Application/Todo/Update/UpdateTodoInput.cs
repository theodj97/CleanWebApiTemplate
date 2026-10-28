using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Application.Todo.Update;

public sealed class UpdateTodoInput
{
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
    public required string UpdatedBy { get; set; }

    /// <summary>Owner filter: the JWT email for non-admins, null for administrators.</summary>
    public string? CreatedBy { get; set; }

    internal bool PatchFields(TodoEntity entityToUpdate)
    {
        bool isUpdated = false;
        if (Title is not null && entityToUpdate.Title != Title)
        {
            entityToUpdate.Title = Title;
            isUpdated = true;
        }
        if (Description is not null)
        {
            entityToUpdate.Description = Description;
            isUpdated = true;
        }
        if (Status is not null)
        {
            entityToUpdate.Status = (int)Status;
            isUpdated = true;
        }
        if (isUpdated)
        {
            entityToUpdate.UpdatedAt = DateTime.UtcNow;
            entityToUpdate.UpdatedBy = UpdatedBy;
        }
        return isUpdated;
    }
}
