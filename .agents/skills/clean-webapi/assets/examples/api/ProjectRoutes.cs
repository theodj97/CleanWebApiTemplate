using System.Net;
using {App}.Api.Common.Abstractions;
using {App}.Api.Configuration;
using {App}.Api.Models.Interfaces;
using {App}.Api.Models.Responses.Project;
using Microsoft.OpenApi;

namespace {App}.Api.Routes.Project;

public sealed class ProjectRoutes : IGroupMap
{
    private const string RouteName = "Project";

    public void MapGroup(IEndpointRouteBuilder app)
    {
        // Every read/write endpoint in this example uses the User policy. A delete endpoint would map a
        // second group with Constants.OPERATOR_POLICY and its own DescribeIdParameter call.
        RouteGroupBuilder userGroup = app.CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.USER_POLICY]);

        DescribeIdParameter(userGroup);

        userGroup.MapGet("/{id}", ProjectEndpoints.GetByIdAsync)
        .WithName("GetProjectById")
        .WithSummary("Get a project by id")
        .WithDescription(
            "Returns a single project. Admins can read any project; every other role only sees projects "
            + "owned by the JWT email. Returns `204 No Content` when the id does not exist or is not "
            + "visible to the caller.")
        .Produces<ProjectResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NoContent, typeof(void));

        userGroup.MapPost("/filter", ProjectEndpoints.FilterAsync)
        .WithName("FilterProjects")
        .WithSummary("Filter projects")
        .WithDescription(
            "Filters the caller's projects by ids, names, statuses and creation dates, with paging and "
            + "sorting. Admins can filter across owners with `createdBys`; every other role is always "
            + "scoped to its own email. Returns `204 No Content` when nothing matches.")
        .Produces<IEnumerable<ProjectResponse>>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NoContent, typeof(void));

        userGroup.MapPost("/", ProjectEndpoints.CreateAsync)
        .WithName("CreateProject")
        .WithSummary("Create a project")
        .WithDescription(
            "Creates a project owned by the authenticated user and returns `201 Created` with the stored "
            + "resource. Names are unique per owner; a duplicate name is rejected with `409 Conflict`.")
        .Produces<ProjectResponse>((int)HttpStatusCode.Created)
        .ProducesProblem((int)HttpStatusCode.Conflict);
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
