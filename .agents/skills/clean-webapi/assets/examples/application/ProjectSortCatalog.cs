using {App}.Application.Dtos.Project;

namespace {App}.Application.Project.Sorting;

/// <summary>
/// The single sort vocabulary for Project queries. The validators, the OpenAPI description and the
/// SQL column map all read this list.
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

    public static string Describe() =>
        string.Join(", ", Keys.Select(key => char.ToLowerInvariant(key[0]) + key[1..]));
}
