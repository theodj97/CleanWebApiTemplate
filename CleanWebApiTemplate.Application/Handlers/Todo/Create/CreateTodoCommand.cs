using CleanWebApiTemplate.Domain.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public sealed record CreateTodoCommand : IRequest<Result<TodoDto?>>
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

internal sealed class CreateTodoCommandHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<CreateTodoCommand, Result<TodoDto?>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoDto?>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoEntity = request.ToEntity();

        var actualUtcMoment = DateTime.UtcNow;
        todoEntity.CreatedAt = actualUtcMoment;
        todoEntity.UpdatedAt = actualUtcMoment;
        todoEntity.Status = (int)ETodoStatus.Pending;

        var todoDb = await repository.CreateAsync(todoEntity, cancellationToken);
        return Result<TodoDto?>.Created(todoDb.ToDto());
    }
}