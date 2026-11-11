using CleanWebApiTemplate.Domain.Common;

namespace CleanWebApiTemplate.Domain.Todo;

/// <summary>A task owned by exactly one caller. The owner never changes.</summary>
public sealed class TodoEntity : BaseEntity<Ulid>
{
    public override Ulid Id { get; set; } = Ulid.NewUlid();
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ETodoStatus Status { get; set; } = ETodoStatus.Pending;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
