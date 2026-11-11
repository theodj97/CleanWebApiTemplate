namespace CleanWebApiTemplate.Application.Project;

/// <summary>
/// The single sort vocabulary for Project queries. Validators, the SQL adapter and the OpenAPI
/// descriptions read this list; the physical column mapping stays in Infrastructure.
/// </summary>
public static class ProjectSortCatalog
{
    public static readonly IReadOnlyList<string> Keys =
    [
        nameof(ProjectDto.Id),
        nameof(ProjectDto.Name),
        nameof(ProjectDto.Description),
        nameof(ProjectDto.CreatedAt),
        nameof(ProjectDto.UpdatedAt),
        nameof(ProjectDto.Status),
        nameof(ProjectDto.CreatedBy),
        nameof(ProjectDto.UpdatedBy),
    ];

    public static IReadOnlySet<string> KeySet { get; } =
        new HashSet<string>(Keys, StringComparer.OrdinalIgnoreCase);

    /// <summary>The keys as a human-readable list for OpenAPI descriptions, e.g. <c>id, name, ...</c>.</summary>
    public static string Describe() =>
        string.Join(", ", Keys.Select(key => char.ToLowerInvariant(key[0]) + key[1..]));
}
