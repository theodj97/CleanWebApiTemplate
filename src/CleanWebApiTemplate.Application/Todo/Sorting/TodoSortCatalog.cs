using CleanWebApiTemplate.Application.Dtos.Todo;

namespace CleanWebApiTemplate.Application.Todo.Sorting;

/// <summary>
/// The single sort vocabulary for Todo queries. Validators, the SQL adapter and the OpenAPI
/// descriptions read this list; the physical column mapping stays in Infrastructure.
/// </summary>
public static class TodoSortCatalog
{
    public static readonly IReadOnlyList<string> Keys =
    [
        nameof(TodoDto.Id),
        nameof(TodoDto.Title),
        nameof(TodoDto.Description),
        nameof(TodoDto.CreatedAt),
        nameof(TodoDto.UpdatedAt),
        nameof(TodoDto.Status),
        nameof(TodoDto.CreatedBy),
        nameof(TodoDto.UpdatedBy),
    ];

    public static IReadOnlySet<string> KeySet { get; } =
        new HashSet<string>(Keys, StringComparer.OrdinalIgnoreCase);

    public static string Describe() =>
        string.Join(", ", Keys.Select(key => char.ToLowerInvariant(key[0]) + key[1..]));
}
