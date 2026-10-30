using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.GetTitles;

public sealed class GetTodoTitlesService(ITodoRepository todoRepository,
                                         IEnumerable<IValidator<GetTodoTitlesInput>> validators)
    : ValidationService, IGetTodoTitlesService
{
    public Task<Result<IEnumerable<TodoDto?>>> GetAsync(GetTodoTitlesInput input, CancellationToken cancellationToken) =>
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
