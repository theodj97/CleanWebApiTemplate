using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo.Update;

public sealed class UpdateTodoHandler(ITodoRepository todoRepository,
                                      ValidatorSet<UpdateTodoInput> validators)
    : ValidationService, IHandler<UpdateTodoInput, TodoDto>
{
    public Task<Result<TodoDto>> HandleAsync(UpdateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            TodoEntity? todoDb = await todoRepository.GetByIdAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            if (todoDb is null)
                return Result<TodoDto>.Failure(new NotFoundError("ID was not found."));

            if (input.Title is not null)
            {
                string trimmedTitle = input.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmedTitle))
                    return Result<TodoDto>.Failure(new BadRequestError("Title can't be empty."));

                // Titles are unique per owner. The owner comes from the stored row, so an admin
                // updating another caller's Todo still checks against that caller's titles.
                if (todoDb.Title != trimmedTitle
                    && await todoRepository.TitleExistsAsync(trimmedTitle,
                                                             todoDb.CreatedBy,
                                                             todoDb.Id,
                                                             cancellationToken))
                {
                    return Result<TodoDto>.Failure(
                        new ConflictError("A Todo with this title already exists for this owner."));
                }
            }

            if (input.PatchFields(todoDb) is false)
                return Result<TodoDto>.Success(todoDb.ToDto());

            Result<bool> update = await todoRepository.UpdateAsync(todoDb, cancellationToken);

            if (update.IsFailure)
                return Result<TodoDto>.Failure(update.Error!);

            return update.Value
                ? Result<TodoDto>.Success(todoDb.ToDto())
                : Result<TodoDto>.Failure(new NotFoundError("ID was not found."));
        }, cancellationToken);
}
