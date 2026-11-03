using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Application.Todo.Create;

public interface ICreateTodoService
{
    Task<Result<TodoDto?>> CreateAsync(CreateTodoInput input, CancellationToken cancellationToken);
}
