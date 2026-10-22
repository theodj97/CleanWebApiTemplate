using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Application.Todo.Delete;

public interface IDeleteTodoService
{
    Task<Result<bool>> DeleteAsync(DeleteTodoInput input, CancellationToken cancellationToken);
}
