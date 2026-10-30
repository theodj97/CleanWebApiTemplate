using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Abstractions;
using CleanWebApiTemplate.Application.Validation;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public sealed class GetTodoByIdService(ITodoRepository todoRepository,
                                       IEnumerable<IValidator<GetTodoByIdInput>> validators)
    : ValidationService, IGetTodoByIdService
{
    public Task<Result<TodoDto?>> GetAsync(GetTodoByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(input.Id), input.CreatedBy, cancellationToken);
            return Result<TodoDto?>.Success(todoDb?.ToDto());
        }, cancellationToken);
}
