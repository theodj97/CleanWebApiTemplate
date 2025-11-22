using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Update;

public sealed class UpdateTodoCommand : IRequest<Result<TodoDto?>>
{
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
    public required string UpdatedBy { get; set; }

    internal bool PatchFields(TodoEntity entityToUpdate)
    {
        bool isUpdated = false;
        if (Title is not null && entityToUpdate.Title != Title)
        {
            entityToUpdate.Title = Title;
            isUpdated = true;
        }
        if (Description is not null)
        {
            entityToUpdate.Description = Description;
            isUpdated = true;
        }
        if (Status is not null)
        {
            entityToUpdate.Status = (int)Status;
            isUpdated = true;
        }
        if (isUpdated)
        {
            entityToUpdate.UpdatedAt = DateTime.UtcNow;
            entityToUpdate.UpdatedBy = UpdatedBy;
        }
        return isUpdated;
    }

}

internal sealed class UpdateTodoCommandHandler(SqlDbContext dbContext) : IRequestHandler<UpdateTodoCommand, Result<TodoDto?>>
{
    private readonly SqlDbContext dbContext = dbContext;

    public async Task<Result<TodoDto?>> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoDb = await dbContext.TodoDb.FirstOrDefaultAsync(x => x.Id == Ulid.Parse(request.Id), cancellationToken);
        if (todoDb is null)
            return Result<TodoDto?>.Failure(new NotFoundError("ID was not found."));

        var isUpdated = request.PatchFields(todoDb);

        if (isUpdated is false)
            return Result<TodoDto?>.Success(todoDb.ToDto());

        var updatedTodo = dbContext.TodoDb.Update(todoDb);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TodoDto?>.Success(updatedTodo.Entity!.ToDto());
    }
}
