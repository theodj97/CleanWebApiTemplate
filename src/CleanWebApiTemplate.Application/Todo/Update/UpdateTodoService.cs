using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Update;

public sealed class UpdateTodoService(ITodoRepository todoRepository,
                                      IEnumerable<IValidator<UpdateTodoInput>> validators)
    : ValidationService, IUpdateTodoService
{
    public Task<Result<TodoDto?>> UpdateAsync(UpdateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(input.Id), cancellationToken);
            if (todoDb is null)
                return Result<TodoDto?>.Failure(new NotFoundError("ID was not found."));

            var isUpdated = input.PatchFields(todoDb);

            if (isUpdated is false)
                return Result<TodoDto?>.Success(todoDb.ToDto());

            await todoRepository.UpdateAsync(todoDb, cancellationToken);
            return Result<TodoDto?>.Success(todoDb.ToDto());
        }, cancellationToken);
}
