using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.Api.Todo.Update;

/// <summary>Request body for updating a Todo. Omitted fields keep their current value.</summary>
public sealed record UpdateTodoRequest
{
    /// <summary>New title. Titles stay unique per owner. Up to 255 characters.</summary>
    public string? Title { get; init; }

    /// <summary>New details for the task. Up to 1000 characters.</summary>
    public string? Description { get; init; }

    /// <summary>New status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public int? Status { get; init; }

    public UpdateTodoInput ToInput(string id, OwnerScope owner) => new()
    {
        Id = id,
        Title = Title,
        Description = Description,
        Status = Status,
        LastWriter = owner.Email,
        VisibleOwner = owner.VisibleOwner,
    };
}
