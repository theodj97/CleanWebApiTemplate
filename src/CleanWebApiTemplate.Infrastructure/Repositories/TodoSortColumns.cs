using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Constants;

namespace CleanWebApiTemplate.Infrastructure.Repositories;

/// <summary>
/// Explicit (reflection-free, Native AOT friendly) whitelist of the properties that can
/// be used to sort Todo queries, mapped to their physical column names. Only columns in
/// this map can reach the ORDER BY clause, so SQL injection through sort input is
/// impossible. Keep it in sync with the valid sort properties declared in the
/// application-layer query validators.
/// </summary>
internal static class TodoSortColumns
{
    public static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TodoEntity.Id)] = TodoTable.IdColumn,
            [nameof(TodoEntity.Title)] = TodoTable.TitleColumn,
            [nameof(TodoEntity.Description)] = TodoTable.DescriptionColumn,
            [nameof(TodoEntity.CreatedAt)] = TodoTable.CreatedAtColumn,
            [nameof(TodoEntity.UpdatedAt)] = TodoTable.UpdatedAtColumn,
            [nameof(TodoEntity.Status)] = TodoTable.StatusColumn,
            [nameof(TodoEntity.CreatedBy)] = TodoTable.CreatedByColumn,
            [nameof(TodoEntity.UpdatedBy)] = TodoTable.UpdatedByColumn,
        };
}
