using {App}.Api.Extensions;
using {App}.Api.Models.Responses.Project;
using {App}.Api.Routes.Project.Create;
using {App}.Api.Routes.Project.Filter;
using {App}.Application.Abstractions.Handlers;
using {App}.Application.Abstractions.Identity;
using {App}.Application.Dtos.Project;
using {App}.Application.Project.Handlers.Create;
using {App}.Application.Project.Handlers.Filtered;
using {App}.Application.Project.Handlers.GetById;
using {App}.Application.Results;

namespace {App}.Api.Routes.Project;

/// <summary>
/// The Project endpoint adapter: request -> input -> handler -> HTTP response lives here, once.
/// <see cref="ProjectRoutes"/> keeps only the route templates and their documentation.
/// </summary>
internal static class ProjectEndpoints
{
    public static async Task<IResult> GetByIdAsync(string id,
                                                   HttpContext context,
                                                   ICurrentUser currentUser,
                                                   IHandler<GetProjectByIdInput, ProjectDto?> handler,
                                                   CancellationToken cancellationToken)
    {
        GetProjectByIdInput input = new() { Id = id, CreatedBy = currentUser.Owner.VisibleOwner };
        Result<ProjectDto?> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectDto?, ProjectResponse>(context);
    }

    public static async Task<IResult> FilterAsync(FilteredProjectRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<FilteredProjectInput, IEnumerable<ProjectDto?>> handler,
                                                  CancellationToken cancellationToken)
    {
        FilteredProjectInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<ProjectDto?>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectDto?, ProjectResponse>(context);
    }

    public static async Task<IResult> CreateAsync(CreateProjectRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<CreateProjectInput, ProjectDto?> handler,
                                                  CancellationToken cancellationToken)
    {
        CreateProjectInput input = request.ToInput(currentUser.Owner);
        Result<ProjectDto?> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToCreatedResponse<ProjectDto?, ProjectResponse>(
            context, dto => $"/api/Project/{dto!.Id}");
    }
}
