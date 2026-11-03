using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public interface IGetTodoByIdService
{
    Task<Result<TodoDto?>> GetAsync(GetTodoByIdInput input, CancellationToken cancellationToken);
}
