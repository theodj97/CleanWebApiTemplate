using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public sealed class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoTitleResponse>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class GetTodoTitleQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoTitleResponse>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<IEnumerable<TodoTitleResponse>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        List<TodoTitleResponse> todosDb = await repository.FilterSortAsync(x => true,
                                                                           p => new TodoTitleResponse { Id = p.Id, Title = p.Title, },
                                                                           request.SortProperties,
                                                                           request.PageNumber,
                                                                           request.PageSize,
                                                                           cancellationToken);

        if (todosDb.Count == 0) return Result<IEnumerable<TodoTitleResponse>>.NoContent();

        return Result<IEnumerable<TodoTitleResponse>>.Success(todosDb!);
    }
}
