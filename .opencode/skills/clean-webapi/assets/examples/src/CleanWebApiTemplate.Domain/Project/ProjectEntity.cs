using CleanWebApiTemplate.Domain.Common;

namespace CleanWebApiTemplate.Domain.Project;

/// <summary>A task owned by exactly one caller. The owner never changes.</summary>
public sealed class ProjectEntity : BaseEntity<Ulid>
{
    public override Ulid Id { get; set; } = Ulid.NewUlid();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public EProjectStatus Status { get; set; } = EProjectStatus.Draft;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
