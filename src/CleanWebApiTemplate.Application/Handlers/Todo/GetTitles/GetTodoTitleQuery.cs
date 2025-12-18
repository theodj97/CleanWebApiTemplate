using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public sealed class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoDto?>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class GetTodoTitleQueryHandler(MariaDbContext dbContext) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoDto?>>>
{
    private readonly MariaDbContext dbContext = dbContext;

    public async Task<Result<IEnumerable<TodoDto?>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        List<TodoDto> projectedTodos = await dbContext.TodoDb.Where(x => true)
                                                             .Select(p => new TodoDto { Id = p.Id, Title = p.Title, })
                                                             .DynamicOrderBy(request.SortProperties)
                                                             .ManagePagination(request.PageNumber, request.PageSize)
                                                             .ToListAsync(cancellationToken);

        return Result<IEnumerable<TodoDto?>>.Success(projectedTodos);
    }
}
