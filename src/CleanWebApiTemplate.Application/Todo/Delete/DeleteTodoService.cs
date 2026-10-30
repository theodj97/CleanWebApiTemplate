using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Abstractions;
using CleanWebApiTemplate.Application.Validation;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.Delete;

public sealed class DeleteTodoService(ITodoRepository todoRepository,
                                      IEnumerable<IValidator<DeleteTodoInput>> validators)
    : ValidationService, IDeleteTodoService
{
    public Task<Result<bool>> DeleteAsync(DeleteTodoInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var result = await todoRepository.DeleteAsync(Ulid.Parse(input.Id), input.CreatedBy, cancellationToken);
            return Result<bool>.Success(result);
        }, cancellationToken);
}
