using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.Create;

/// <summary>Input of the create use case. <see cref="Owner"/> is the caller, never a request body value.</summary>
public sealed record CreateProjectInput
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string Owner { get; init; }

    internal ProjectEntity ToEntity() => new()
    {
        Id = Ulid.NewUlid(),
        Name = Name.Trim(),
        Description = Description,
        Status = EProjectStatus.Draft,
        CreatedBy = Owner,
        UpdatedBy = Owner,
    };
}
