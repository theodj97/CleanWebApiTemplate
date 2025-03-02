namespace CleanWebApiTemplate.Domain.Models.Responses;

public record TodoResponse
{
    public required Ulid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int Status { get; set; }
    public required string CreatedBy { get; set; }
}
