using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using CustomMediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public sealed record GetTodoByIdQuery : IRequest<Result<TodoDto?>>
{
    public required string Id { get; set; }
}

internal sealed class GetTodoByIdQueryHandler(IBaseQueryRepository<TodoEntity, Ulid> repository) : IRequestHandler<GetTodoByIdQuery, Result<TodoDto?>>
{
    private readonly IBaseQueryRepository<TodoEntity, Ulid> repository = repository;

    public async Task<Result<TodoDto?>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.GetByIdAsync(Ulid.Parse(request.Id), cancellationToken);
        return Result<TodoDto?>.Success(todoDb?.ToDto());
    }
}
