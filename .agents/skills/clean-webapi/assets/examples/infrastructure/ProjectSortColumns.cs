using {App}.Domain.Models.Entities;

namespace {App}.Infrastructure.Repositories.Project.Helpers;

/// <summary>
/// Explicit (reflection-free, Native AOT friendly) map of the Project sort keys to their physical
/// column names. Only columns in this map can reach the ORDER BY clause. The accepted keys live in
/// <c>ProjectSortCatalog</c>; <c>ProjectSortColumnsTests</c> asserts both sets are identical.
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
