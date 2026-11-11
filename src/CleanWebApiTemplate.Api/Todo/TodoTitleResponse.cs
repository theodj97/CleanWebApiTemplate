using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Application.Todo;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>The id and title projection of a Todo.</summary>
public sealed record TodoTitleResponse : IBaseResponse<TodoTitle, TodoTitleResponse>
{
    /// <summary>ULID identifier.</summary>
    public required Ulid Id { get; init; }

    /// <summary>Task title.</summary>
    public required string Title { get; init; }

    public static TodoTitleResponse ToResponseModel(TodoTitle dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
    };
}
