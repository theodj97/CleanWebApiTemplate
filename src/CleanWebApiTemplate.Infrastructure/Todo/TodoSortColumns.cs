using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Infrastructure.Todo;

/// <summary>
/// Explicit (reflection-free, Native AOT friendly) whitelist of the properties that can be used to sort
/// Todo queries, mapped to their physical column names. Only columns in this map can reach the ORDER BY
/// clause, so SQL injection through sort input is impossible. Keep it aligned with
/// <see cref="Application.Todo.TodoSortCatalog"/>; a unit test proves both name the same keys.
/// </summary>
public static class TodoSortColumns
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
