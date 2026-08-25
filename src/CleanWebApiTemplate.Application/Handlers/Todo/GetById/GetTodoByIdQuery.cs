using CleanWebApiTemplate.Application.CQRS.Messages;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetById;

public sealed record GetTodoByIdQuery : IQuery<Result<TodoDto?>>
{
    public required string Id { get; set; }
}

internal sealed class GetTodoByIdQueryHandler(ITodoRepository todoRepository) : IQueryHandler<GetTodoByIdQuery, Result<TodoDto?>>
{
    private readonly ITodoRepository todoRepository = todoRepository;

    public async Task<Result<TodoDto?>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await todoRepository.GetByIdAsync(Ulid.Parse(request.Id), cancellationToken);
        return Result<TodoDto?>.Success(todoDb?.ToDto());
    }
}
