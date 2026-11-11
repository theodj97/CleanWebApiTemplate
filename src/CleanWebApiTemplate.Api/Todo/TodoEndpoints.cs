using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Todo.Create;
using CleanWebApiTemplate.Api.Todo.Filter;
using CleanWebApiTemplate.Api.Todo.Get;
using CleanWebApiTemplate.Api.Todo.Update;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Application.Todo.Delete;
using CleanWebApiTemplate.Application.Todo.Filter;
using CleanWebApiTemplate.Application.Todo.GetById;
using CleanWebApiTemplate.Application.Todo.GetTitles;
using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>
/// The Todo endpoint adapter: request -> input -> handler -> HTTP response lives here, once.
/// <see cref="TodoRoutes"/> keeps only the route templates and their documentation.
/// </summary>
internal static class TodoEndpoints
{
    public static async Task<IResult> GetByIdAsync(string id,
                                                   HttpContext context,
                                                   ICurrentUser currentUser,
                                                   IHandler<GetTodoByIdInput, TodoDto> handler,
                                                   CancellationToken cancellationToken)
    {
        GetTodoByIdInput input = new() { Id = id, VisibleOwner = currentUser.Owner.VisibleOwner };
        Result<TodoDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto, TodoResponse>(context);
    }

    public static async Task<IResult> FilterAsync(FilteredTodoRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<FilteredTodoInput, IEnumerable<TodoDto>> handler,
                                                  CancellationToken cancellationToken)
    {
        FilteredTodoInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<TodoDto>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto, TodoResponse>(context);
    }

    public static async Task<IResult> GetTitlesAsync(GetTodoTitlesRequest request,
                                                     HttpContext context,
                                                     ICurrentUser currentUser,
                                                     IHandler<GetTodoTitlesInput, IEnumerable<TodoTitle>> handler,
                                                     CancellationToken cancellationToken)
    {
        GetTodoTitlesInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<TodoTitle>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoTitle, TodoTitleResponse>(context);
    }

    public static async Task<IResult> CreateAsync(CreateTodoRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<CreateTodoInput, TodoDto> handler,
                                                  CancellationToken cancellationToken)
    {
        CreateTodoInput input = request.ToInput(currentUser.Owner);
        Result<TodoDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToCreatedResponse<TodoDto, TodoResponse>(context, dto => $"/api/todos/{dto.Id}");
    }

    public static async Task<IResult> UpdateAsync(string id,
                                                  UpdateTodoRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<UpdateTodoInput, TodoDto> handler,
                                                  CancellationToken cancellationToken)
    {
        UpdateTodoInput input = request.ToInput(id, currentUser.Owner);
        Result<TodoDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto, TodoResponse>(context);
    }

    public static async Task<IResult> DeleteAsync(string id,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<DeleteTodoInput, bool> handler,
                                                  CancellationToken cancellationToken)
    {
        DeleteTodoInput input = new() { Id = id, VisibleOwner = currentUser.Owner.VisibleOwner };
        Result<bool> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToNoContentResponse(context);
    }
}
