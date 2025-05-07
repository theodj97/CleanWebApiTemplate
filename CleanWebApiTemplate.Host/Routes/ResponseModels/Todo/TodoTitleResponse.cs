using CleanWebApiTemplate.Domain.Dtos.Todo;

namespace CleanWebApiTemplate.Host.Routes.ResponseModels.Todo;

internal sealed record TodoTitleResponse : IBaseResponse<TodoDto?, TodoTitleResponse>
{
    public required Ulid Id { get; set; }
    public required string Title { get; set; }

    public static TodoTitleResponse? ToResponse(TodoDto? dto)
    {
        if (dto is null) return null;
        return new()
        {
            Id = (Ulid)dto.Id!,
            Title = dto.Title!,
        };
    }
}
