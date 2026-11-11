using {App}.Domain.Models.Entities;

namespace {App}.Application.Project.Handlers.Create;

public sealed record CreateProjectInput
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string CreatedBy { get; init; }

    internal ProjectEntity ToEntity() => new()
    {
        Id = Ulid.NewUlid(),
        Name = Name.Trim(),
        Description = Description,
        CreatedBy = CreatedBy,
        UpdatedBy = CreatedBy
    };
}
