using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public sealed class GetTodoByIdHandler(ITodoRepository todoRepository,
                                       ValidatorSet<GetTodoByIdInput> validators)
    : ValidationService, IHandler<GetTodoByIdInput, TodoDto>
{
    public Task<Result<TodoDto>> HandleAsync(GetTodoByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            TodoEntity? todo = await todoRepository.GetByIdAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            return todo is null
                ? Result<TodoDto>.Failure(new NotFoundError("ID was not found."))
                : Result<TodoDto>.Success(todo.ToDto());
        }, cancellationToken);
}
