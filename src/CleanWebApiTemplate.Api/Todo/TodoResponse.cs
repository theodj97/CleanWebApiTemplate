using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Application.Todo;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>A Todo item returned by the API.</summary>
public sealed record TodoResponse : IBaseResponse<TodoDto, TodoResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; init; }

    /// <summary>Task title.</summary>
    public required string Title { get; init; }

    /// <summary>Task details.</summary>
    public required string Description { get; init; }

    /// <summary>Creation timestamp (UTC).</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Last update timestamp (UTC).</summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>Status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public required int Status { get; init; }

    /// <summary>Owner email.</summary>
    public required string CreatedBy { get; init; }

    /// <summary>Email of the last writer.</summary>
    public required string UpdatedBy { get; init; }

    public static TodoResponse ToResponseModel(TodoDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        Status = (int)dto.Status,
        CreatedBy = dto.CreatedBy,
        UpdatedBy = dto.UpdatedBy,
    };
}
