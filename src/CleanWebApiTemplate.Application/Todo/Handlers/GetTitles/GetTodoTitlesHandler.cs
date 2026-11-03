using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Projections;
using CleanWebApiTemplate.Application.Validation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;

public sealed class GetTodoTitlesHandler(ITodoRepository todoRepository,
                                         ValidatorSet<GetTodoTitlesInput> validators)
    : ValidationService, IHandler<GetTodoTitlesInput, IEnumerable<TodoTitle>>
{
    public Task<Result<IEnumerable<TodoTitle>>> HandleAsync(GetTodoTitlesInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var projectedTodos = await todoRepository.GetTitlesAsync(input.PageNumber,
                                                                     input.PageSize,
                                                                     input.SortProperties,
                                                                     input.CreatedBy,
                                                                     cancellationToken);

            return Result<IEnumerable<TodoTitle>>.Success(projectedTodos);
        }, cancellationToken);
}
