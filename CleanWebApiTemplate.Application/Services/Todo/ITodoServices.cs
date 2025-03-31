using System.Linq.Expressions;
using CleanWebApiTemplate.Domain.Models.Entities;

namespace CleanWebApiTemplate.Application.Services.Todo;

public interface ITodoServices
{
    public Expression<Func<TodoEntity, object>> TodoResolveOrderBy(byte? orderBy);
}
