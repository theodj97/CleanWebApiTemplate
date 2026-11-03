using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Validation;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;

public sealed class GetTodoTitlesHandler(ITodoRepository todoRepository,
                                         IEnumerable<IValidator<GetTodoTitlesInput>> validators)
    : ValidationService, IHandler<GetTodoTitlesInput, IEnumerable<TodoDto?>>
{
    public Task<Result<IEnumerable<TodoDto?>>> HandleAsync(GetTodoTitlesInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var projectedTodos = await todoRepository.GetTitlesAsync(input.PageNumber,
                                                                     input.PageSize,
                                                                     input.SortProperties,
                                                                     input.CreatedBy,
                                                                     cancellationToken);

            return Result<IEnumerable<TodoDto?>>.Success(projectedTodos.Select(todo => todo.ToTitleDto()));
        }, cancellationToken);
}
