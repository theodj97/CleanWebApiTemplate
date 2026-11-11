using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Infrastructure.Project;

/// <summary>
/// Explicit (reflection-free, Native AOT friendly) whitelist of the properties that can be used to sort
/// Project queries, mapped to their physical column names. Only columns in this map can reach the ORDER BY
/// clause, so SQL injection through sort input is impossible. Keep it aligned with
/// <see cref="Application.Project.ProjectSortCatalog"/>; a unit test proves both name the same keys.
/// </summary>
public static class ProjectSortColumns
{
    public static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ProjectEntity.Id)] = ProjectTable.IdColumn,
            [nameof(ProjectEntity.Name)] = ProjectTable.NameColumn,
            [nameof(ProjectEntity.Description)] = ProjectTable.DescriptionColumn,
            [nameof(ProjectEntity.CreatedAt)] = ProjectTable.CreatedAtColumn,
            [nameof(ProjectEntity.UpdatedAt)] = ProjectTable.UpdatedAtColumn,
            [nameof(ProjectEntity.Status)] = ProjectTable.StatusColumn,
            [nameof(ProjectEntity.CreatedBy)] = ProjectTable.CreatedByColumn,
            [nameof(ProjectEntity.UpdatedBy)] = ProjectTable.UpdatedByColumn,
        };
}
