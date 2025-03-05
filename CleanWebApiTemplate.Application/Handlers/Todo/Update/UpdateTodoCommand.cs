using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Update;

public class UpdateTodoCommand : IRequest<Result<TodoResponse>>
{
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public byte? Status { get; set; }
    public required string UpdatedBy { get; set; }
}

internal class UpdateTodoCommandHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<UpdateTodoCommand, Result<TodoResponse>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoResponse>> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.GetByIdAsyncANT(request.Id, cancellationToken);
        if (todoDb is null)
            return Result<TodoResponse>.Failure(new NotFoundError("ID was not found."));

        var (isUpdated, entityToUpdate) = TodoMappers.FromCommandToEntity(request, todoDb);

        if (isUpdated is false)
            return Result<TodoResponse>.Success(TodoMappers.FromEntityToResponse(todoDb));

        var updatedTodo = await repository.UpdateAsync(entityToUpdate, cancellationToken);
        return Result<TodoResponse>.Success(TodoMappers.FromEntityToResponse(updatedTodo));
    }
}