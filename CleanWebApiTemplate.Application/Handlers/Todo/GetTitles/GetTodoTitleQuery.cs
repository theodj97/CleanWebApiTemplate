using CleanWebApiTemplate.Domain.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Infrastructure.Common;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;

public sealed class GetTodoTitleQuery : IRequest<Result<IEnumerable<TodoDto>>>
{
    public byte? PageNumber { get; set; }
    public byte? PageSize { get; set; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; } = null;
}

internal sealed class GetTodoTitleQueryHandler(IBaseRepository<TodoEntity> repository) : IRequestHandler<GetTodoTitleQuery, Result<IEnumerable<TodoDto>>>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    public async Task<Result<IEnumerable<TodoDto>>> Handle(GetTodoTitleQuery request, CancellationToken cancellationToken)
    {
        List<TodoDto> projectedTodos = await repository.FilterSortAsync(x => true,
                                                                        p => new TodoDto { Id = p.Id, Title = p.Title, },
                                                                        request.SortProperties,
                                                                        request.PageNumber,
                                                                        request.PageSize,
                                                                        cancellationToken);

        return Result<IEnumerable<TodoDto>>.Success(projectedTodos);
    }
}
