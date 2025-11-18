using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using Microsoft.EntityFrameworkCore;
using MinimalWebApiCleanExtensions.ResultPattern;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public sealed record GetTodoByIdQuery : IRequest<Result<TodoDto?>>
{
    public required string Id { get; set; }
}

internal sealed class GetTodoByIdQueryHandler(SqlDbContext dbContext) : IRequestHandler<GetTodoByIdQuery, Result<TodoDto?>>
{
    private readonly SqlDbContext dbContext = dbContext;

    public async Task<Result<TodoDto?>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await dbContext.TodoDb.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Ulid.Parse(request.Id), cancellationToken);
        return Result<TodoDto?>.Success(todoDb?.ToDto());
    }
}
