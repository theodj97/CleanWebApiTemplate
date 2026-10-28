using CleanWebApiTemplate.Domain.Models.Dtos.Todo;

namespace CleanWebApiTemplate.Api.Models.Responses.Todo;

/// <summary>A TODO item returned by the API.</summary>
public sealed record TodoResponse : IBaseResponse<TodoDto?, TodoResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; set; }

    /// <summary>Task title.</summary>
    public required string Title { get; set; }

    /// <summary>Task details.</summary>
    public required string Description { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>Status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public required int Status { get; set; }

    /// <summary>Owner email.</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Email of the last writer.</summary>
    public required string UpdatedBy { get; set; }

    public static TodoResponse? ToResponseModel(TodoDto? dto)
    {
        if (dto is null) return null;
        return new()
        {
            Id = (Ulid)dto.Id!,
            Title = dto.Title!,
            Description = dto.Description ?? string.Empty,
            CreatedAt = (DateTime)dto.CreatedAt!,
            Status = (int)dto.Status!,
            CreatedBy = dto.CreatedBy!,
            UpdatedBy = dto.UpdatedBy!
        };
    }
}
