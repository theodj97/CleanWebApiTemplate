using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public sealed class GetTodoByIdQuery : IRequest<Result<TodoResponse>>
{
    public required string Id { get; set; }
}

internal sealed class GetTodoByIdQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoByIdQuery, Result<TodoResponse>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (todoDb is null)
            return Result<TodoResponse>.NoContent();

        return Result<TodoResponse>.Success(TodoMappers.FromEntityToResponse(todoDb));
    }
}