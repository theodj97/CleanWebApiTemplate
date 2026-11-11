using System.Net;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>The Todo routes: templates, policies, metadata and documented status codes.</summary>
public sealed class TodoRoutes : IGroupMap
{
    private const string Route = "todos";
    private const string Tag = "Todos";

    public void MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder userGroup = app.CreateAuthorizedRouteGroupBuilder(Route, Tag, [Constants.UserPolicy]);
        RouteGroupBuilder operatorGroup = app.CreateAuthorizedRouteGroupBuilder(Route, Tag, [Constants.OperatorPolicy]);

        DescribeIdParameter(userGroup);
        DescribeIdParameter(operatorGroup);

        userGroup.MapGet("/{id}", TodoEndpoints.GetByIdAsync)
        .WithName("GetTodoById")
        .WithSummary("Get a Todo by id")
        .WithDescription(
            "Returns a single Todo. Admins can read any Todo; every other role only sees Todos owned "
            + "by the JWT email. Returns `404 Not Found` when the id does not exist or is not visible "
            + "to the caller.")
        .Produces<TodoResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.NotFound);

        userGroup.MapPost("/filter", TodoEndpoints.FilterAsync)
        .WithName("FilterTodos")
        .WithSummary("Filter Todos")
        .WithDescription(
            "Filters the caller's Todos by ids, titles, statuses and creation dates, with paging and "
            + "sorting. Admins can filter across owners with `owners`; every other role is always "
            + "scoped to its own email. Returns `200 OK` with an empty array when nothing matches.")
        .Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/titles", TodoEndpoints.GetTitlesAsync)
        .WithName("GetTodoTitles")
        .WithSummary("List Todo titles")
        .WithDescription(
            "Returns the id and title projection of the caller's Todos. Admins see every owner; other "
            + "roles see their own. Supports paging and sorting. Returns `200 OK` with an empty array "
            + "when there are no rows.")
        .Produces<IEnumerable<TodoTitleResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/", TodoEndpoints.CreateAsync)
        .WithName("CreateTodo")
        .WithSummary("Create a Todo")
        .WithDescription(
            "Creates a Todo owned by the authenticated user and returns `201 Created` with the stored "
            + "resource. Titles are unique per owner; a duplicate title is rejected with "
            + "`409 Conflict`.")
        .Produces<TodoResponse>((int)HttpStatusCode.Created)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        userGroup.MapPut("/{id}", TodoEndpoints.UpdateAsync)
        .WithName("UpdateTodo")
        .WithSummary("Update a Todo")
        .WithDescription(
            "Updates the supplied fields of a Todo. Admins can update any Todo; other roles only their "
            + "own. Returns `404 Not Found` when the id does not exist or is not visible, "
            + "`400 Bad Request` for invalid fields or an unknown status, and `409 Conflict` when the "
            + "new title is already used by another Todo of the same owner.")
        .Produces<TodoResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        operatorGroup.MapDelete("/{id}", TodoEndpoints.DeleteAsync)
        .WithName("DeleteTodo")
        .WithSummary("Delete a Todo")
        .WithDescription(
            "Deletes a Todo. Requires the Operator or Admin role. Operators can delete their own "
            + "Todos; admins can delete any. Returns `204 No Content` when a row was deleted and "
            + "`404 Not Found` when no row matched.")
        .Produces((int)HttpStatusCode.NoContent)
        .ProducesProblem((int)HttpStatusCode.NotFound);
    }

    private static void DescribeIdParameter(RouteGroupBuilder group) =>
        group.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.Parameters is null)
                return Task.CompletedTask;

            foreach (var parameter in operation.Parameters)
            {
                if (parameter is OpenApiParameter { Name: "id", In: ParameterLocation.Path } id)
                    id.Description = "Todo identifier (26-character ULID).";
            }

            return Task.CompletedTask;
        });
}
