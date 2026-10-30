using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Application.Todo.GetTitles;

public interface IGetTodoTitlesService
{
    Task<Result<IEnumerable<TodoDto?>>> GetAsync(GetTodoTitlesInput input, CancellationToken cancellationToken);
}
