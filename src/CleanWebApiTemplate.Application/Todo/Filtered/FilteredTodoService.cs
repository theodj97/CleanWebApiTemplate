using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Filtered;

public sealed class FilteredTodoService(ITodoRepository todoRepository,
                                        IEnumerable<IValidator<FilteredTodoInput>> validators)
    : ValidationService, IFilteredTodoService
{
    public Task<Result<IEnumerable<TodoDto?>>> FilterAsync(FilteredTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todosDb = await todoRepository.SearchAsync(input.ToFilter(), cancellationToken);

            return Result<IEnumerable<TodoDto?>>.Success(todosDb.Select(x => x.ToDto()));
        }, cancellationToken);
}
