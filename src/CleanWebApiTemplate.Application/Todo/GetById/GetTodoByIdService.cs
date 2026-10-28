using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo.GetById;

public sealed class GetTodoByIdService(ITodoRepository todoRepository,
                                       IEnumerable<IValidator<GetTodoByIdInput>> validators)
    : ValidationService, IGetTodoByIdService
{
    public Task<Result<TodoDto?>> GetAsync(GetTodoByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(input.Id), input.CreatedBy, cancellationToken);
            return Result<TodoDto?>.Success(todoDb?.ToDto());
        }, cancellationToken);
}
