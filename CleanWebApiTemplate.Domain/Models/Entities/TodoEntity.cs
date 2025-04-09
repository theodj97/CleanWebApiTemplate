using CleanWebApiTemplate.Domain.Models.Enums.Todo;

namespace CleanWebApiTemplate.Domain.Models.Entities;

public class TodoEntity : BaseEntity
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Status { get; set; } = (int)ETodoStatus.Pending;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
