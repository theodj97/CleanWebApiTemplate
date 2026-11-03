using CleanWebApiTemplate.Application.Dtos.Todo;

namespace CleanWebApiTemplate.Api.Models.Responses.Todo;

/// <summary>The id and title projection of a TODO.</summary>
public sealed record TodoTitleResponse : IBaseResponse<TodoDto?, TodoTitleResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; set; }

    /// <summary>Task title.</summary>
    public required string Title { get; set; }

    public static TodoTitleResponse? ToResponseModel(TodoDto? dto)
    {
        if (dto is null) return null;
        return new()
        {
            Id = (Ulid)dto.Id!,
            Title = dto.Title!,
        };
    }
}
