using CleanWebApiTemplate.Application.Mappers.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Helpers;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Create;

public class CreateTodoCommand : IRequest<Result<TodoResponse>>
{
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string CreatedBy { get; init; }
}

internal class CreateTodoCommandHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<CreateTodoCommand, Result<TodoResponse>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<TodoResponse>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todoEntity = TodoMappers.FromCommandToEntity(request);
        var todoDb = await repository.CreateAsync(todoEntity, cancellationToken);

        if (todoDb is null)
            return Result<TodoResponse>.NoContent();

        return Result<TodoResponse>.Success(TodoMappers.FromEntityToResponse(todoDb));
    }
}