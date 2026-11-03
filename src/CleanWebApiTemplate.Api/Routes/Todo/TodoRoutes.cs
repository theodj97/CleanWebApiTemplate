using System.Net;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Models.Interfaces;
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
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Routes.Todo;

public sealed class TodoRoutes(ICurrentUser currentUser) : IGroupMap
{
    private const string RouteName = "Todo";

    public void MapGroup(IEndpointRouteBuilder app)
    {
        var userGroup = app.CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.USER_POLICY]);
        var operatorGroup = app.CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.OPERATOR_POLICY]);

        DescribeIdParameter(userGroup);
        DescribeIdParameter(operatorGroup);

        userGroup.MapGet("/{id}", async (string id,
                                         IHandler<GetTodoByIdInput, TodoDto?> handler,
                                         CancellationToken cancellationToken) =>
        {
            GetTodoByIdInput input = new() { Id = id, CreatedBy = currentUser.OwnerScope };
            Result<TodoDto?> result = await handler.HandleAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        })
        .WithName("GetTodoById")
        .WithSummary("Get a TODO by id")
        .WithDescription(
            "Returns a single TODO. Admins can read any TODO; every other role only sees TODOs owned "
            + "by the JWT email. Returns `204 No Content` when the id does not exist or is not visible "
            + "to the caller.")
        .Produces<TodoResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NoContent, typeof(void));

        userGroup.MapPost("/filter", async (FilteredTodoRequest request,
                                            IHandler<FilteredTodoInput, IEnumerable<TodoDto?>> handler,
                                            CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(currentUser.OwnerScope);
            var result = await handler.HandleAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        })
        .WithName("FilterTodos")
        .WithSummary("Filter TODOs")
        .WithDescription(
            "Filters the caller's TODOs by ids, titles, statuses and creation dates, with paging and "
            + "sorting. Admins can filter across owners with `createdBy`; every other role is always "
            + "scoped to its own email. Returns `204 No Content` when nothing matches.")
        .Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NoContent, typeof(void));

        userGroup.MapPost("/titles", async (GetTodoTitlesRequest request,
                                            IHandler<GetTodoTitlesInput, IEnumerable<TodoDto?>> handler,
                                            CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(currentUser.OwnerScope);
            var result = await handler.HandleAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoTitleResponse>();
        })
        .WithName("GetTodoTitles")
        .WithSummary("List TODO titles")
        .WithDescription(
            "Returns the id and title projection of the caller's TODOs. Admins see every owner; other "
            + "roles see their own. Supports paging and sorting. Returns `204 No Content` when there "
            + "are no rows.")
        .Produces<IEnumerable<TodoTitleResponse>>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NoContent, typeof(void));

        userGroup.MapPost("/", async (CreateTodoRequest request,
                                      IHandler<CreateTodoInput, TodoDto?> handler,
                                      CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(currentUser.Email);
            var result = await handler.HandleAsync(input, cancellationToken);
            return result.ToCreatedResponse<TodoDto?, TodoResponse>(dto => $"/api/Todo/{dto!.Id}");
        })
        .WithName("CreateTodo")
        .WithSummary("Create a TODO")
        .WithDescription(
            "Creates a TODO owned by the authenticated user and returns `201 Created` with the stored "
            + "resource. Titles are unique per owner; a duplicate title is rejected with "
            + "`409 Conflict`.")
        .Produces<TodoResponse>((int)HttpStatusCode.Created)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        userGroup.MapPut("/{id}", async (string id,
                                         UpdateTodoRequest request,
                                         IHandler<UpdateTodoInput, TodoDto?> handler,
                                         CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(id, currentUser.Email, currentUser.OwnerScope);
            var result = await handler.HandleAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        })
        .WithName("UpdateTodo")
        .WithSummary("Update a TODO")
        .WithDescription(
            "Updates the supplied fields of a TODO. Admins can update any TODO; other roles only their "
            + "own. Returns `404 Not Found` when the id does not exist or is not visible, "
            + "`400 Bad Request` for invalid fields or an unknown status, and `409 Conflict` when the "
            + "new title is already used by another TODO of the same owner.")
        .Produces<TodoResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        operatorGroup.MapDelete("/{id}", async (string id,
                                                IHandler<DeleteTodoInput, bool> handler,
                                                CancellationToken cancellationToken) =>
        {
            var input = new DeleteTodoInput() { Id = id, CreatedBy = currentUser.OwnerScope };
            var result = await handler.HandleAsync(input, cancellationToken);
            return result.ToResponse();
        })
        .WithName("DeleteTodo")
        .WithSummary("Delete a TODO")
        .WithDescription(
            "Deletes a TODO. Requires the Operator or Admin role. Operators can delete their own "
            + "TODOs; admins can delete any. Returns `true` when a row was deleted and `false` when "
            + "no row matched.")
        .Produces<bool>((int)HttpStatusCode.OK);
    }

    private static void DescribeIdParameter(RouteGroupBuilder group) =>
        group.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.Parameters is null)
                return Task.CompletedTask;

            foreach (var parameter in operation.Parameters)
            {
                if (parameter is OpenApiParameter { Name: "id", In: ParameterLocation.Path } id)
                    id.Description = "TODO identifier (26-character ULID).";
            }

            return Task.CompletedTask;
        });
}
