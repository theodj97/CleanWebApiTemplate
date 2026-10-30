using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Application.Todo.Update;

public interface IUpdateTodoService
{
    Task<Result<TodoDto?>> UpdateAsync(UpdateTodoInput input, CancellationToken cancellationToken);
}
