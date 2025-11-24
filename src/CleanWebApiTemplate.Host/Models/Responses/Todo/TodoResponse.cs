using CleanWebApiTemplate.Domain.Models.Dtos.Todo;

namespace CleanWebApiTemplate.Host.Models.Responses.Todo;

public sealed record TodoResponse : IBaseResponse<TodoDto?, TodoResponse>
{
    public required Ulid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int Status { get; set; }
    public required string CreatedBy { get; set; }
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
