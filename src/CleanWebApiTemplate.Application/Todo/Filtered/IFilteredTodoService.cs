using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Application.Todo.Filtered;

public interface IFilteredTodoService
{
    Task<Result<IEnumerable<TodoDto?>>> FilterAsync(FilteredTodoInput input, CancellationToken cancellationToken);
}
