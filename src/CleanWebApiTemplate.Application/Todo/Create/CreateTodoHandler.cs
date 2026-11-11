using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.Create;

public sealed class CreateTodoHandler(ITodoRepository todoRepository,
                                      ValidatorSet<CreateTodoInput> validators)
    : ValidationService, IHandler<CreateTodoInput, TodoDto>
{
    public Task<Result<TodoDto>> HandleAsync(CreateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            TodoEntity todoEntity = input.ToEntity();

            DateTime actualUtcMoment = DateTime.UtcNow;
            todoEntity.CreatedAt = actualUtcMoment;
            todoEntity.UpdatedAt = actualUtcMoment;

            // Uniqueness is a business rule, not input validation, so the handler owns it and
            // reports a conflict. The unique index still protects the race below.
            if (await todoRepository.TitleExistsAsync(todoEntity.Title,
                                                     todoEntity.CreatedBy,
                                                     cancellationToken: cancellationToken))
            {
                return Result<TodoDto>.Failure(
                    new ConflictError("A Todo with this title already exists for this owner."));
            }

            Result<TodoEntity> insertion = await todoRepository.InsertAsync(todoEntity, cancellationToken);

            return insertion.IsFailure
                ? Result<TodoDto>.Failure(insertion.Error!)
                : Result<TodoDto>.Success(insertion.Value!.ToDto());
        }, cancellationToken);
}
