using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoTitleResponse>>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

internal class GetTodoTitleQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoTitleResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<IEnumerable<TodoTitleResponse>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.FilterAsyncANT(x => true,
                                                     p => new TodoTitleResponse
                                                     {
                                                         Id = p.Id,
                                                         Title = p.Title,
                                                     },
                                                     request.PageNumber,
                                                     request.PageSize,
                                                     cancellationToken);
        return Result<IEnumerable<TodoTitleResponse>>.Success(todoDb);
    }
}
