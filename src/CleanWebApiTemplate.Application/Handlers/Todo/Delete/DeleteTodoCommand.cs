using CleanWebApiTemplate.Application.CQRS.Messages;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public sealed record DeleteTodoCommand : ICommand<Result<bool>>
{
    public required string Id { get; set; }
}

internal sealed class DeleteTodoCommandHandler(ITodoRepository todoRepository) : ICommandHandler<DeleteTodoCommand, Result<bool>>
{
    private readonly ITodoRepository todoRepository = todoRepository;

    public async Task<Result<bool>> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var result = await todoRepository.DeleteAsync(Ulid.Parse(request.Id), cancellationToken);
        return Result<bool>.Success(result);
    }
}
