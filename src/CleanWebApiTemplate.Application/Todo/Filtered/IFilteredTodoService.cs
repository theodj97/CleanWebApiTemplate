using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Application.Todo.Filtered;

public interface IFilteredTodoService
{
    Task<Result<IEnumerable<TodoDto?>>> FilterAsync(FilteredTodoInput input, CancellationToken cancellationToken);
}
