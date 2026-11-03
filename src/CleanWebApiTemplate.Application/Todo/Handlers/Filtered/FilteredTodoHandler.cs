using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Validation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.Filtered;

public sealed class FilteredTodoHandler(ITodoRepository todoRepository,
                                        ValidatorSet<FilteredTodoInput> validators)
    : ValidationService, IHandler<FilteredTodoInput, IEnumerable<TodoDto?>>
{
    public Task<Result<IEnumerable<TodoDto?>>> HandleAsync(FilteredTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todosDb = await todoRepository.SearchAsync(input.ToFilter(), cancellationToken);

            return Result<IEnumerable<TodoDto?>>.Success(todosDb.Select(x => x.ToDto()));
        }, cancellationToken);
}
