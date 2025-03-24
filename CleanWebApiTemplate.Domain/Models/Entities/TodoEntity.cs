namespace CleanWebApiTemplate.Domain.Models.Entities;

public class TodoEntity : BaseEntity
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required int Status { get; set; }
    public required string CreatedBy { get; set; }
    public required string UpdatedBy { get; set; }
}
