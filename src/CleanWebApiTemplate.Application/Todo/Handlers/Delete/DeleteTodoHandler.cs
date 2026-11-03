using System.Globalization;
using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Validation;

namespace CleanWebApiTemplate.Application.Todo.Handlers.Delete;

public sealed class DeleteTodoHandler(ITodoRepository todoRepository,
                                      ValidatorSet<DeleteTodoInput> validators)
    : ValidationService, IHandler<DeleteTodoInput, bool>
{
    public Task<Result<bool>> HandleAsync(DeleteTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var result = await todoRepository.DeleteAsync(Ulid.Parse(input.Id, CultureInfo.InvariantCulture), input.CreatedBy, cancellationToken);
            return Result<bool>.Success(result);
        }, cancellationToken);
}
