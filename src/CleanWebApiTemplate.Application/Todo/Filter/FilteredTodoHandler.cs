using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.Filter;

public sealed class FilteredTodoHandler(ITodoRepository todoRepository,
                                        ValidatorSet<FilteredTodoInput> validators)
    : ValidationService, IHandler<FilteredTodoInput, IEnumerable<TodoDto>>
{
    public Task<Result<IEnumerable<TodoDto>>> HandleAsync(FilteredTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            IReadOnlyList<TodoEntity> todos = await todoRepository.SearchAsync(input.ToFilter(), cancellationToken);

            return Result<IEnumerable<TodoDto>>.Success(todos.Select(todo => todo.ToDto()));
        }, cancellationToken);
}
