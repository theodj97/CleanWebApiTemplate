using CleanWebApiTemplate.Application.Abstractions.Messages;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public sealed class GetTodoTitleQuery : IQuery<Result<IEnumerable<TodoDto?>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class GetTodoTitleQueryHandler(SqlDbContext dbContext) : IQueryHandler<GetTodoTitleQuery, Result<IEnumerable<TodoDto?>>>
{
    private readonly SqlDbContext dbContext = dbContext;

    // The member-init projection (Expression.Bind) is conservatively flagged, but the
    // TodoDto property accessors are directly referenced (ldtoken) by this same method
    // body, so they cannot be trimmed away.
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The projected TodoDto property setters are statically referenced by the expression tree built in this method.")]
    public async Task<Result<IEnumerable<TodoDto?>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        List<TodoDto> projectedTodos = await dbContext.TodoDb.Where(x => true)
                                                             .Select(p => new TodoDto { Id = p.Id, Title = p.Title, })
                                                             .DynamicOrderBy(request.SortProperties, TodoSortProperties.Dto)
                                                             .ManagePagination(request.PageNumber, request.PageSize)
                                                             .ToListAsync(cancellationToken);

        return Result<IEnumerable<TodoDto?>>.Success(projectedTodos);
    }
}
