using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;

namespace CleanWebApiTemplate.Application.Todo.Delete;

public sealed class DeleteTodoHandler(ITodoRepository todoRepository,
                                      ValidatorSet<DeleteTodoInput> validators)
    : ValidationService, IHandler<DeleteTodoInput, bool>
{
    public Task<Result<bool>> HandleAsync(DeleteTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            bool deleted = await todoRepository.DeleteAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            return deleted
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new NotFoundError("ID was not found."));
        }, cancellationToken);
}
