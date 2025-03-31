using CleanWebApiTemplate.Application.Services.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoTitleResponse>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public byte? OrderBy { get; set; } = null;
    public bool OrderDescending { get; set; } = false;
}

internal class GetTodoTitleQueryHandler(IBaseRepository<TodoEntity> repository, ITodoServices todoServices) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoTitleResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;
    private readonly ITodoServices todoServices = todoServices;


    public async Task<Result<IEnumerable<TodoTitleResponse>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        var todoDb = await repository.FilterAsyncANT(x => true,
                                                     p => new TodoTitleResponse
                                                     {
                                                         Id = p.Id,
                                                         Title = p.Title,
                                                     },
                                                     todoServices.TodoResolveOrderBy(request.OrderBy),
                                                     request.OrderDescending,
                                                     request.PageNumber,
                                                     request.PageSize,
                                                     cancellationToken);

        return Result<IEnumerable<TodoTitleResponse>>.Success(todoDb);
    }
}
