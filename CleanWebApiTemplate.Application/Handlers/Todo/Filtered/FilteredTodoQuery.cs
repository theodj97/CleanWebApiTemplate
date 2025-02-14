using CleanWebApiTemplate.Application.Mappers.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Helpers;
using MediatR;
using System.Linq.Expressions;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Filtered;

public class FilteredTodoQuery : IRequest<Result<IEnumerable<TodoResponse>>>
{
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<int>? Status { get; set; }
    public IEnumerable<string>? CreatedBy { get; set; }
}

internal class FilteredTodoQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<FilteredTodoQuery, Result<IEnumerable<TodoResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;
    public async Task<Result<IEnumerable<TodoResponse>>> Handle(FilteredTodoQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<TodoEntity, bool>> filter = x => true;
        var queryBody = filter.Body;
        var parameter = filter.Parameters[0];

        if (request.Title is not null && request.Title.Any())
        {
            Expression<Func<TodoEntity, bool>> titleFilter = x => request.Title.Contains(x.Title);
            queryBody = Expression.AndAlso(queryBody, titleFilter.Body);
        }

        if (request.Status is not null && request.Status.Any())
        {
            Expression<Func<TodoEntity, bool>> statusFilter = x => request.Status.Contains((int)x.Status);
            queryBody = Expression.AndAlso(queryBody, statusFilter.Body);
        }

        if (request.CreatedBy is not null && request.CreatedBy.Any())
        {
            Expression<Func<TodoEntity, bool>> createdByFilter = x => request.CreatedBy.Contains(x.CreatedBy);
            queryBody = Expression.AndAlso(queryBody, createdByFilter.Body);
        }

        filter = Expression.Lambda<Func<TodoEntity, bool>>(queryBody, parameter);
        var todosDb = await repository.FilterAsyncANT(filter, cancellationToken);
        if (todosDb is null)
            return Result<IEnumerable<TodoResponse>>.NoContent();

        return Result<IEnumerable<TodoResponse>>.Success(TodoMappers.FromEntityToResponse(todosDb));
    }
}