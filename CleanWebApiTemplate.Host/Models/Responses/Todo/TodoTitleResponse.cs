using CleanWebApiTemplate.Domain.Models.Dtos.Todo;

namespace CleanWebApiTemplate.Host.Models.Responses.Todo;

public sealed record TodoTitleResponse : IBaseResponse<TodoDto?, TodoTitleResponse>
{
    public required Ulid Id { get; set; }
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
