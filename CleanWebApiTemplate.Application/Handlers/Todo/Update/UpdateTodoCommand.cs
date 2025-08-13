using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using CustomMediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Update;

public sealed class UpdateTodoCommand : IRequest<Result<TodoDto?>>
{
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
    public required string UpdatedBy { get; set; }

    internal (bool isUpdated, TodoEntity updatedEntity) PatchFields(TodoEntity entityToUpdate)
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
        return (isUpdated, entityToUpdate);
    }

}

internal sealed class UpdateTodoCommandHandler(IBaseQueryRepository<TodoEntity, Ulid> queryRepository,
                                               IBaseCommandRepository<TodoEntity, Ulid> commandRepository) : IRequestHandler<UpdateTodoCommand, Result<TodoDto?>>
{
    private readonly IBaseQueryRepository<TodoEntity, Ulid> queryRepository = queryRepository;

    private readonly IBaseCommandRepository<TodoEntity, Ulid> commandRepository = commandRepository;

    public async Task<Result<TodoDto?>> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoDb = await queryRepository.GetByIdAsync(Ulid.Parse(request.Id), cancellationToken);
        if (todoDb is null)
            return Result<TodoDto?>.Failure(new NotFoundError("ID was not found."));

        var (isUpdated, entityToUpdate) = request.PatchFields(todoDb);

        if (isUpdated is false)
            return Result<TodoDto?>.Success(todoDb.ToDto());

        var updatedTodo = await commandRepository.UpdateAsync(entityToUpdate, cancellationToken);
        return Result<TodoDto?>.Success(updatedTodo!.ToDto());
    }
}