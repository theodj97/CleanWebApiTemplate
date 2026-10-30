using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Create;

public sealed class CreateTodoService(ITodoRepository todoRepository,
                                      IEnumerable<IValidator<CreateTodoInput>> validators)
    : ValidationService, ICreateTodoService
{
    public Task<Result<TodoDto?>> CreateAsync(CreateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoEntity = input.ToEntity();

            var actualUtcMoment = DateTime.UtcNow;
            todoEntity.CreatedAt = actualUtcMoment;
            todoEntity.UpdatedAt = actualUtcMoment;
            todoEntity.Status = (int)ETodoStatus.Pending;

            // Uniqueness is a business rule, not input validation, so the service owns it and
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
                : Result<TodoDto?>.Created(insertion.Value!.ToDto());
        }, cancellationToken);
}
