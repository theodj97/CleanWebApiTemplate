using System.Globalization;
using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Validation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.GetById;

public sealed class GetTodoByIdHandler(ITodoRepository todoRepository,
                                       ValidatorSet<GetTodoByIdInput> validators)
    : ValidationService, IHandler<GetTodoByIdInput, TodoDto?>
{
    public Task<Result<TodoDto?>> HandleAsync(GetTodoByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(input.Id, CultureInfo.InvariantCulture), input.CreatedBy, cancellationToken);
            return Result<TodoDto?>.Success(todoDb?.ToDto());
        }, cancellationToken);
}
