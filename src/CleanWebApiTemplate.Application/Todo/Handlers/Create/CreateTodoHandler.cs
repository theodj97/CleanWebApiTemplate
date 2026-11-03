using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;

namespace CleanWebApiTemplate.Application.Todo.Handlers.Create;

public sealed class CreateTodoHandler(ITodoRepository todoRepository,
                                      ValidatorSet<CreateTodoInput> validators)
    : ValidationService, IHandler<CreateTodoInput, TodoDto?>
{
    public Task<Result<TodoDto?>> HandleAsync(CreateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoEntity = input.ToEntity();

            var actualUtcMoment = DateTime.UtcNow;
            todoEntity.CreatedAt = actualUtcMoment;
            todoEntity.UpdatedAt = actualUtcMoment;
            todoEntity.Status = (int)ETodoStatus.Pending;

            // Uniqueness is a business rule, not input validation, so the handler owns it and
            // reports a conflict. The unique index still protects the race below.
            if (await todoRepository.TitleExistsAsync(todoEntity.Title,
                                                     todoEntity.CreatedBy,
                                                     cancellationToken: cancellationToken))
            {
                return Result<TodoDto?>.Failure(
                    new ConflictError("A TODO with this title already exists for this owner."));
            }

            var insertion = await todoRepository.InsertAsync(todoEntity, cancellationToken);

            return insertion.IsFailure
                ? Result<TodoDto?>.Failure(insertion.Error!)
                : Result<TodoDto?>.Success(insertion.Value!.ToDto());
        }, cancellationToken);
}
