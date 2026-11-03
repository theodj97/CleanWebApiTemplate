using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.Api.Routes.Todo.Update;

/// <summary>Request body for updating a TODO. Omitted fields keep their current value.</summary>
public sealed class UpdateTodoRequest
{
    /// <summary>New title. Titles stay unique per owner. Up to 255 characters.</summary>
    public string? Title { get; set; }

    /// <summary>New details for the task. Up to 1000 characters.</summary>
    public string? Description { get; set; }

    /// <summary>New status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public int? Status { get; set; }

    public UpdateTodoInput ToInput(string id, string userEmail, string? ownerScope) => new()
    {
        Id = id,
        Title = Title,
        Description = Description,
        Status = Status,
        UpdatedBy = userEmail,
        CreatedBy = ownerScope
    };
}
