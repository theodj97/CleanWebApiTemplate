using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public sealed class CreateTodoCommand : IRequest<Result<TodoResponse>>
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string CreatedBy { get; init; }
}

internal sealed class CreateTodoCommandHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<CreateTodoCommand, Result<TodoResponse>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoResponse>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoEntity = TodoMappers.FromCommandToEntity(request);

        var actualUtcMoment = DateTime.UtcNow;
        todoEntity.CreatedAt = actualUtcMoment;
        todoEntity.UpdatedAt = actualUtcMoment;
        todoEntity.Status = (int)ETodoStatus.Pending;

        var todoDb = await repository.CreateAsync(todoEntity, cancellationToken);

        return Result<TodoResponse>.Created(TodoMappers.FromEntityToResponse(todoDb));
    }
}