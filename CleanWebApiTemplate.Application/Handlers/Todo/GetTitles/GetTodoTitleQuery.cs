using System.Linq.Expressions;
using System.Reflection;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;
using Microsoft.IdentityModel.Tokens;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoTitleResponse>>>
{
    public byte PageNumber { get; set; }
    public byte PageSize { get; set; }
    public string? OrderBy { get; set; } = null;
    public bool OrderDescending { get; set; } = false;
}

internal class GetTodoTitleQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoTitleResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<IEnumerable<TodoTitleResponse>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<TodoTitleResponse, object>> orderSelector;

        if (request.OrderBy.IsNullOrEmpty())
            orderSelector = o => o.Title;

        else
        {
            var propertyInfo = typeof(TodoTitleResponse).GetProperty(request.OrderBy!, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(typeof(TodoTitleResponse), "o");
            var propertyAccess = Expression.Property(parameter, propertyInfo!);
            var converted = Expression.Convert(propertyAccess, typeof(object));
            orderSelector = Expression.Lambda<Func<TodoTitleResponse, object>>(converted, parameter);
        }

        var todoDb = await repository.FilterAsyncANT(x => true,
                                                     p => new TodoTitleResponse
                                                     {
                                                         Id = p.Id,
                                                         Title = p.Title,
                                                     },
                                                     orderSelector,
                                                     request.OrderDescending,
                                                     request.PageNumber,
                                                     request.PageSize,
                                                     cancellationToken);

        return Result<IEnumerable<TodoTitleResponse>>.Success(todoDb);
    }
}
