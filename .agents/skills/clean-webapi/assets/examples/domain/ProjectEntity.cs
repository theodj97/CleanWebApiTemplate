using {App}.Domain.Models.Enums.Project;

namespace {App}.Domain.Models.Entities;

/// <summary>A unit of work owned by one user. The name is unique per owner.</summary>
public sealed class ProjectEntity : BaseEntity<Ulid>
{
    public override Ulid Id { get; set; } = Ulid.NewUlid();

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int Status { get; set; } = (int)EProjectStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}
