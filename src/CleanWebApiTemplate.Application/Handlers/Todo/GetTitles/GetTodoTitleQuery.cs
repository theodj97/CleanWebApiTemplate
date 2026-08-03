using CleanWebApiTemplate.Application.CQRS.Messages;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public sealed class GetTodoTitleQuery : IQuery<Result<IEnumerable<TodoDto?>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class GetTodoTitleQueryHandler(ITodoRepository todoRepository) : IQueryHandler<GetTodoTitleQuery, Result<IEnumerable<TodoDto?>>>
{
    private readonly ITodoRepository todoRepository = todoRepository;

    public async Task<Result<IEnumerable<TodoDto?>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        var projectedTodos = await todoRepository.GetTitlesAsync(request.PageNumber,
                                                                 request.PageSize,
                                                                 request.SortProperties,
                                                                 cancellationToken);

        return Result<IEnumerable<TodoDto?>>.Success(projectedTodos);
    }
}
