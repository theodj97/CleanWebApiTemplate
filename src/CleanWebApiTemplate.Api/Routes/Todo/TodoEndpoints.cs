using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Api.Routes.Todo.Create;
using CleanWebApiTemplate.Api.Routes.Todo.Filter;
using CleanWebApiTemplate.Api.Routes.Todo.Get;
using CleanWebApiTemplate.Api.Routes.Todo.Update;
using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Identity;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Handlers.Create;
using CleanWebApiTemplate.Application.Todo.Handlers.Delete;
using CleanWebApiTemplate.Application.Todo.Handlers.Filtered;
using CleanWebApiTemplate.Application.Todo.Handlers.GetById;
using CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;
using CleanWebApiTemplate.Application.Todo.Handlers.Update;
using CleanWebApiTemplate.Application.Todo.Projections;

namespace CleanWebApiTemplate.Api.Routes.Todo;

/// <summary>
/// The Todo endpoint adapter: request -> input -> handler -> HTTP response lives here, once.
/// <see cref="TodoRoutes"/> keeps only the route templates and their documentation.
/// </summary>
internal static class TodoEndpoints
{
    public static async Task<IResult> GetByIdAsync(string id,
                                                   HttpContext context,
                                                   ICurrentUser currentUser,
                                                   IHandler<GetTodoByIdInput, TodoDto?> handler,
                                                   CancellationToken cancellationToken)
    {
        GetTodoByIdInput input = new() { Id = id, CreatedBy = currentUser.Owner.VisibleOwner };
        Result<TodoDto?> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto?, TodoResponse>(context);
    }

    public static async Task<IResult> FilterAsync(FilteredTodoRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<FilteredTodoInput, IEnumerable<TodoDto?>> handler,
                                                  CancellationToken cancellationToken)
    {
        FilteredTodoInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<TodoDto?>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto?, TodoResponse>(context);
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
                                                  IHandler<CreateTodoInput, TodoDto?> handler,
                                                  CancellationToken cancellationToken)
    {
        CreateTodoInput input = request.ToInput(currentUser.Owner);
        Result<TodoDto?> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToCreatedResponse<TodoDto?, TodoResponse>(context, dto => $"/api/Todo/{dto!.Id}");
    }

    public static async Task<IResult> UpdateAsync(string id,
                                                  UpdateTodoRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<UpdateTodoInput, TodoDto?> handler,
                                                  CancellationToken cancellationToken)
    {
        UpdateTodoInput input = request.ToInput(id, currentUser.Owner);
        Result<TodoDto?> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<TodoDto?, TodoResponse>(context);
    }

    public static async Task<IResult> DeleteAsync(string id,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<DeleteTodoInput, bool> handler,
                                                  CancellationToken cancellationToken)
    {
        DeleteTodoInput input = new() { Id = id, CreatedBy = currentUser.Owner.VisibleOwner };
        Result<bool> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse(context);
    }
}
