using CleanWebApiTemplate.Application.Services.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Filtered;

public class FilteredTodoQuery : IRequest<Result<IEnumerable<TodoResponse>>>
{
    public IEnumerable<string>? Ids { get; set; }
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<int>? Status { get; set; } = [];
    public IEnumerable<string>? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public byte? OrderBy { get; set; } = null;
    public bool OrderDescending { get; set; } = false;
}

internal class FilteredTodoQueryHandler(IBaseRepository<TodoEntity> repository, ITodoServices todoServices) : IRequestHandler<FilteredTodoQuery, Result<IEnumerable<TodoResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;
    private readonly ITodoServices todoServices = todoServices;


    public async Task<Result<IEnumerable<TodoResponse>>> Handle(FilteredTodoQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<TodoEntity, bool>> filter = x => true;
        var queryBody = filter.Body;
        var parameter = filter.Parameters[0];

        if (request.Ids is not null && request.Ids.Any())
        {
            var idsParsed = request.Ids.Select(x => Ulid.Parse(x));

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

        var todosDb = await repository.FilterAsyncANT(filter,
                                                      todoServices.TodoResolveOrderBy(request.OrderBy),
                                                      request.OrderDescending,
                                                      request.PageNumber,
                                                      request.PageSize,
                                                      cancellationToken: cancellationToken);
        if (todosDb is null)
            return Result<IEnumerable<TodoResponse>>.NoContent();

        return Result<IEnumerable<TodoResponse>>.Success(TodoMappers.FromEntityToResponse(todosDb));
    }
}