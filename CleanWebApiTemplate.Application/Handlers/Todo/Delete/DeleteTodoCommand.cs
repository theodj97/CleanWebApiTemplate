using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Delete;

public sealed record DeleteTodoCommand : IRequest<Result<bool>>
{
    public required string Id { get; set; }
}

internal sealed class DeleteTodoCommandHandler(IBaseCommandRepository<TodoEntity> repository) : IRequestHandler<DeleteTodoCommand, Result<bool>>
{
    private readonly IBaseCommandRepository<TodoEntity> repository = repository;

    public async Task<Result<bool>> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var result = await repository.DeleteAsync(request.Id, cancellationToken);
        return Result<bool>.Success(result);
    }
}
