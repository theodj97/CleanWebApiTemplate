using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using CustomMediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Filtered;

public sealed record FilteredTodoQuery : IRequest<Result<IEnumerable<TodoDto?>>>
{
    public IEnumerable<string>? Ids { get; set; }
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<int>? Status { get; set; } = [];
    public IEnumerable<string>? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class FilteredTodoQueryHandler(MariaDbContext dbContext) : IRequestHandler<FilteredTodoQuery, Result<IEnumerable<TodoDto?>>>
{
    private readonly MariaDbContext dbContext = dbContext;

    public async Task<Result<IEnumerable<TodoDto?>>> Handle(FilteredTodoQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<TodoEntity, bool>> filter = x => true;
        var queryBody = filter.Body;
        var parameter = filter.Parameters[0];

        if (request.Ids is not null && request.Ids.Any())
        {
            var idsParsed = request.Ids.Select(Ulid.Parse);

            Expression<Func<TodoEntity, bool>> idFilter = x => idsParsed.Contains(x.Id);
            queryBody = Expression.AndAlso(queryBody, Expression.Invoke(idFilter, parameter));
        }

        if (request.Title is not null && request.Title.Any())
        {
            Expression<Func<TodoEntity, bool>> titleFilter = x => request.Title.Contains(x.Title);
            queryBody = Expression.AndAlso(queryBody, Expression.Invoke(titleFilter, parameter));
        }

        if (request.Status is not null && request.Status.Any())
        {
            Expression<Func<TodoEntity, bool>> statusFilter = x => request.Status.Contains(x.Status);
            queryBody = Expression.AndAlso(queryBody, Expression.Invoke(statusFilter, parameter));
        }

        if (request.CreatedBy is not null && request.CreatedBy.Any())
        {
            Expression<Func<TodoEntity, bool>> createdByFilter = x => request.CreatedBy.Contains(x.CreatedBy);
            queryBody = Expression.AndAlso(queryBody, Expression.Invoke(createdByFilter, parameter));
        }

        if (string.IsNullOrEmpty(request.StartDate) is false && string.IsNullOrEmpty(request.EndDate) is false)
        {
            var startDate = DateTime.Parse(request.StartDate);
            var endDate = DateTime.Parse(request.EndDate);

            Expression<Func<TodoEntity, bool>> startDateFilter = x => x.CreatedAt >= startDate && x.CreatedAt <= endDate;
            queryBody = Expression.AndAlso(queryBody, Expression.Invoke(startDateFilter, parameter));
        }

        filter = Expression.Lambda<Func<TodoEntity, bool>>(queryBody, parameter);

        var todosDb = await dbContext.TodoDb.Where(filter)
                                            .AsNoTracking()
                                            .DynamicOrderBy(request.SortProperties)
                                            .ManagePagination(request.PageNumber, request.PageSize)
                                            .ToListAsync(cancellationToken);

        return Result<IEnumerable<TodoDto?>>.Success(todosDb.Select(x => x.ToDto()));
    }
}