using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using MinimalWebApiCleanExtensions.ResultPattern;

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

internal sealed class CreateTodoCommandHandler(SqlDbContext dbContext) : IRequestHandler<CreateTodoCommand, Result<TodoDto?>>
{
    private readonly SqlDbContext dbContext = dbContext;

    public async Task<Result<TodoDto?>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoEntity = request.ToEntity();

        var actualUtcMoment = DateTime.UtcNow;
        todoEntity.CreatedAt = actualUtcMoment;
        todoEntity.UpdatedAt = actualUtcMoment;
        todoEntity.Status = (int)ETodoStatus.Pending;

        var resultDb = await dbContext.TodoDb.AddAsync(todoEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TodoDto?>.Created(resultDb.Entity.ToDto());
    }
}