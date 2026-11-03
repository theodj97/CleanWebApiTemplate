using CleanWebApiTemplate.Application.Todo.Projections;

namespace CleanWebApiTemplate.Api.Models.Responses.Todo;

/// <summary>The id and title projection of a TODO.</summary>
public sealed record TodoTitleResponse : IBaseResponse<TodoTitle, TodoTitleResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; set; }

    /// <summary>Task title.</summary>
    public required string Title { get; set; }

    public static TodoTitleResponse? ToResponseModel(TodoTitle? dto)
    {
        if (dto is null) return null;
        return new()
        {
            Id = dto.Id,
            Title = dto.Title,
        };
    }
}
