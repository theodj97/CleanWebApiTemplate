using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

/// <summary>
/// Explicit (reflection-free, Native AOT friendly) maps of the properties that can be
/// used to sort Todo queries. Keep them in sync with the valid sort properties
/// declared in the query validators.
/// </summary>
internal static class TodoSortProperties
{
    public static readonly IReadOnlyDictionary<string, Expression<Func<TodoEntity, object>>> Entity =
        new Dictionary<string, Expression<Func<TodoEntity, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TodoEntity.Id)] = x => x.Id,
            [nameof(TodoEntity.Title)] = x => x.Title,
            [nameof(TodoEntity.Description)] = x => x.Description,
            [nameof(TodoEntity.CreatedAt)] = x => x.CreatedAt,
            [nameof(TodoEntity.UpdatedAt)] = x => x.UpdatedAt,
            [nameof(TodoEntity.Status)] = x => x.Status,
            [nameof(TodoEntity.CreatedBy)] = x => x.CreatedBy,
            [nameof(TodoEntity.UpdatedBy)] = x => x.UpdatedBy,
        };

    public static readonly IReadOnlyDictionary<string, Expression<Func<TodoDto, object>>> Dto =
        new Dictionary<string, Expression<Func<TodoDto, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TodoDto.Id)] = x => x.Id!,
            [nameof(TodoDto.Title)] = x => x.Title!,
            [nameof(TodoDto.Description)] = x => x.Description!,
            [nameof(TodoDto.CreatedAt)] = x => x.CreatedAt!,
            [nameof(TodoDto.UpdatedAt)] = x => x.UpdatedAt!,
            [nameof(TodoDto.Status)] = x => x.Status!,
            [nameof(TodoDto.CreatedBy)] = x => x.CreatedBy!,
            [nameof(TodoDto.UpdatedBy)] = x => x.UpdatedBy!,
        };
}
