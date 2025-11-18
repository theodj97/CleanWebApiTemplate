using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using Microsoft.EntityFrameworkCore;
using MinimalWebApiCleanExtensions.ResultPattern;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public sealed record DeleteTodoCommand : IRequest<Result<bool>>
{
    public required string Id { get; set; }
}

internal sealed class DeleteTodoCommandHandler(SqlDbContext dbContext) : IRequestHandler<DeleteTodoCommand, Result<bool>>
{
    private readonly SqlDbContext dbContext = dbContext;

    public async Task<Result<bool>> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var result = (await dbContext.TodoDb.Where(x => x.Id!.Equals(Ulid.Parse(request.Id)))
            .ExecuteDeleteAsync(cancellationToken: cancellationToken)) > 0;
        return Result<bool>.Success(result);
    }
}
