using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;

namespace CleanWebApiTemplate.Application.Todo.GetTitles;

public sealed class GetTodoTitlesHandler(ITodoRepository todoRepository,
                                         ValidatorSet<GetTodoTitlesInput> validators)
    : ValidationService, IHandler<GetTodoTitlesInput, IEnumerable<TodoTitle>>
{
    public Task<Result<IEnumerable<TodoTitle>>> HandleAsync(GetTodoTitlesInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            IReadOnlyList<TodoTitle> projectedTodos = await todoRepository.GetTitlesAsync(
                input.PageNumber,
                input.PageSize,
                input.SortProperties,
                input.VisibleOwner,
                cancellationToken);

            return Result<IEnumerable<TodoTitle>>.Success(projectedTodos);
        }, cancellationToken);
}
