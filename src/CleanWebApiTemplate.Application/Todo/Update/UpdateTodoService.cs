using System.Globalization;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Abstractions;
using CleanWebApiTemplate.Application.Validation;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Update;

public sealed class UpdateTodoService(ITodoRepository todoRepository,
                                      IEnumerable<IValidator<UpdateTodoInput>> validators)
    : ValidationService, IUpdateTodoService
{
    public Task<Result<TodoDto?>> UpdateAsync(UpdateTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(input.Id, CultureInfo.InvariantCulture), input.CreatedBy, cancellationToken);
            if (todoDb is null)
                return Result<TodoDto?>.Failure(new NotFoundError("ID was not found."));

            if (input.Title is not null)
            {
                string trimmedTitle = input.Title.Trim();
                if (string.IsNullOrWhiteSpace(trimmedTitle))
                    return Result<TodoDto?>.Failure(new BadRequestError("Title can't be empty."));

                // Titles are unique per owner. The owner comes from the stored row, so an admin
                // updating another user's TODO still checks against that user's titles.
                if (todoDb.Title != trimmedTitle
                    && await todoRepository.TitleExistsAsync(trimmedTitle,
                                                             todoDb.CreatedBy,
                                                             todoDb.Id,
                                                             cancellationToken))
                {
                    return Result<TodoDto?>.Failure(
                        new ConflictError("A TODO with this title already exists for this owner."));
                }
            }

            var isUpdated = input.PatchFields(todoDb);

            if (isUpdated is false)
                return Result<TodoDto?>.Success(todoDb.ToDto());

            var update = await todoRepository.UpdateAsync(todoDb, cancellationToken);

            if (update.IsFailure)
                return Result<TodoDto?>.Failure(update.Error!);

            return update.Value
                ? Result<TodoDto?>.Success(todoDb.ToDto())
                : Result<TodoDto?>.Failure(new NotFoundError("ID was not found."));
        }, cancellationToken);
}
