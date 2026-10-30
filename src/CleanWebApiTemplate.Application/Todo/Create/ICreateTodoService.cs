using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Application.Todo.Create;

public interface ICreateTodoService
{
    Task<Result<TodoDto?>> CreateAsync(CreateTodoInput input, CancellationToken cancellationToken);
}
