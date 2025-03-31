using System.Linq.Expressions;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;

namespace CleanWebApiTemplate.Application.Services.Todo;

public class TodoServices : ITodoServices
{
    public Expression<Func<TodoEntity, object>> TodoResolveOrderBy(byte? orderBy) => (ETodoOrderBy?)orderBy switch
    {
        ETodoOrderBy.Id => o => o.Id,
        ETodoOrderBy.Title => o => o.Title,
        ETodoOrderBy.Description => o => o.Description,
        ETodoOrderBy.Status => o => o.Status,
        ETodoOrderBy.CreatedAt => o => o.CreatedAt,
        ETodoOrderBy.UpdatedBy => o => o.UpdatedBy,
        ETodoOrderBy.CreatedBy => o => o.CreatedBy,
        null => o => o.Id,
        _ => throw new ArgumentOutOfRangeException(nameof(orderBy), $"Invalid validation order: {orderBy}")
    };
}
