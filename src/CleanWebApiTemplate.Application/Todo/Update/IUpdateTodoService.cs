using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Application.Todo.Update;

public interface IUpdateTodoService
{
    Task<Result<TodoDto?>> UpdateAsync(UpdateTodoInput input, CancellationToken cancellationToken);
}
