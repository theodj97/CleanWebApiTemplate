namespace CleanWebApiTemplate.Domain.Dtos.Todo;

public record TodoDto
{
    public Ulid? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? Status { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
