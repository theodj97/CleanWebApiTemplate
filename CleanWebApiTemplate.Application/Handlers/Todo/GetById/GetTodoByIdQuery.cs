using CleanWebApiTemplate.Application.Mappers.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public class GetTodoByIdQuery : IRequest<Result<TodoResponse>>
{
    public required string Id { get; set; }
}

internal class GetTodoByIdQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoByIdQuery, Result<TodoResponse>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.GetByIdAsyncANT(request.Id, cancellationToken);
        if (todoDb is null)
            return Result<TodoResponse>.NoContent();

        return Result<TodoResponse>.Success(TodoMappers.FromEntityToResponse(todoDb));
    }
}