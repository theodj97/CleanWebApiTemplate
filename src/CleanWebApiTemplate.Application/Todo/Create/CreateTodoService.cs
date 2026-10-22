using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
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

            var createdTodo = await todoRepository.InsertAsync(todoEntity, cancellationToken);
            return Result<TodoDto?>.Created(createdTodo.ToDto());
        }, cancellationToken);
}
