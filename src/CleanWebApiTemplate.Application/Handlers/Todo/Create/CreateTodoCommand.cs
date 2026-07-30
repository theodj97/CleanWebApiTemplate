using CleanWebApiTemplate.Application.CQRS.Messages;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public sealed record CreateTodoCommand : ICommand<Result<TodoDto?>>
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string CreatedBy { get; init; }

    internal TodoEntity ToEntity() => new()
    {
        Id = Ulid.NewUlid(),
        Title = Title,
        Description = Description,
        CreatedBy = CreatedBy,
        UpdatedBy = CreatedBy
    };
}

internal sealed class CreateTodoCommandHandler(ITodoRepository todoRepository) : ICommandHandler<CreateTodoCommand, Result<TodoDto?>>
{
    private readonly ITodoRepository todoRepository = todoRepository;

    public async Task<Result<TodoDto?>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoEntity = request.ToEntity();

        var actualUtcMoment = DateTime.UtcNow;
        todoEntity.CreatedAt = actualUtcMoment;
        todoEntity.UpdatedAt = actualUtcMoment;
        todoEntity.Status = (int)ETodoStatus.Pending;

        var createdTodo = await todoRepository.InsertAsync(todoEntity, cancellationToken);
        return Result<TodoDto?>.Created(createdTodo.ToDto());
    }
}