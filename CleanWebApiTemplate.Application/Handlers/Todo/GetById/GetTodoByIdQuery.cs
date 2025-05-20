using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public sealed record GetTodoByIdQuery : IRequest<Result<TodoDto?>>
{
    public required string Id { get; set; }
}

internal sealed class GetTodoByIdQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoByIdQuery, Result<TodoDto?>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoDto?>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.GetByIdAsync(request.Id, cancellationToken);
        return Result<TodoDto?>.Success(todoDb?.ToDto());
    }
}