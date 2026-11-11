namespace CleanWebApiTemplate.Application.Project;

/// <summary>The id/name projection of a Project. Only these two fields exist by design.</summary>
public sealed record ProjectName(Ulid Id, string Name);
