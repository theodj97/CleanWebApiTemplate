using System.Net;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>The Project routes: templates, policies, metadata and documented status codes.</summary>
public sealed class ProjectRoutes : IGroupMap
{
    private const string Route = "projects";
    private const string Tag = "Projects";

    public void MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder userGroup = app.CreateAuthorizedRouteGroupBuilder(Route, Tag, [Constants.UserPolicy]);
        RouteGroupBuilder operatorGroup = app.CreateAuthorizedRouteGroupBuilder(Route, Tag, [Constants.OperatorPolicy]);

        DescribeIdParameter(userGroup);
        DescribeIdParameter(operatorGroup);

        userGroup.MapGet("/{id}", ProjectEndpoints.GetByIdAsync)
        .WithName("GetProjectById")
        .WithSummary("Get a Project by id")
        .WithDescription(
            "Returns a single Project. Admins can read any Project; every other role only sees Projects owned "
            + "by the JWT email. Returns `404 Not Found` when the id does not exist or is not visible "
            + "to the caller.")
        .Produces<ProjectResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.NotFound);

        userGroup.MapPost("/filter", ProjectEndpoints.FilterAsync)
        .WithName("FilterProjects")
        .WithSummary("Filter Projects")
        .WithDescription(
            "Filters the caller's Projects by ids, names, statuses and creation dates, with paging and "
            + "sorting. Admins can filter across owners with `owners`; every other role is always "
            + "scoped to its own email. Returns `200 OK` with an empty array when nothing matches.")
        .Produces<IEnumerable<ProjectResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/names", ProjectEndpoints.GetNamesAsync)
        .WithName("GetProjectNames")
        .WithSummary("List Project names")
        .WithDescription(
            "Returns the id and name projection of the caller's Projects. Admins see every owner; other "
            + "roles see their own. Supports paging and sorting. Returns `200 OK` with an empty array "
            + "when there are no rows.")
        .Produces<IEnumerable<ProjectNameResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/", ProjectEndpoints.CreateAsync)
        .WithName("CreateProject")
        .WithSummary("Create a Project")
        .WithDescription(
            "Creates a Project owned by the authenticated user and returns `201 Created` with the stored "
            + "resource. Names are unique per owner; a duplicate name is rejected with "
            + "`409 Conflict`.")
        .Produces<ProjectResponse>((int)HttpStatusCode.Created)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        userGroup.MapPut("/{id}", ProjectEndpoints.UpdateAsync)
        .WithName("UpdateProject")
        .WithSummary("Update a Project")
        .WithDescription(
            "Updates the supplied fields of a Project. Admins can update any Project; other roles only their "
            + "own. Returns `404 Not Found` when the id does not exist or is not visible, "
            + "`400 Bad Request` for invalid fields or an unknown status, and `409 Conflict` when the "
            + "new name is already used by another Project of the same owner.")
        .Produces<ProjectResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.Conflict);

        operatorGroup.MapDelete("/{id}", ProjectEndpoints.DeleteAsync)
        .WithName("DeleteProject")
        .WithSummary("Delete a Project")
        .WithDescription(
            "Deletes a Project. Requires the Operator or Admin role. Operators can delete their own "
            + "Projects; admins can delete any. Returns `204 No Content` when a row was deleted and "
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
                    id.Description = "Project identifier (26-character ULID).";
            }

            return Task.CompletedTask;
        });
}
