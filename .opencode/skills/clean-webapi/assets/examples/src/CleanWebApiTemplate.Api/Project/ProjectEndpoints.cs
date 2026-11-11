using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Project.Create;
using CleanWebApiTemplate.Api.Project.Filter;
using CleanWebApiTemplate.Api.Project.Get;
using CleanWebApiTemplate.Api.Project.Update;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.Create;
using CleanWebApiTemplate.Application.Project.Delete;
using CleanWebApiTemplate.Application.Project.Filter;
using CleanWebApiTemplate.Application.Project.GetById;
using CleanWebApiTemplate.Application.Project.GetNames;
using CleanWebApiTemplate.Application.Project.Update;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>
/// The Project endpoint adapter: request -> input -> handler -> HTTP response lives here, once.
/// <see cref="ProjectRoutes"/> keeps only the route templates and their documentation.
/// </summary>
internal static class ProjectEndpoints
{
    public static async Task<IResult> GetByIdAsync(string id,
                                                   HttpContext context,
                                                   ICurrentUser currentUser,
                                                   IHandler<GetProjectByIdInput, ProjectDto> handler,
                                                   CancellationToken cancellationToken)
    {
        GetProjectByIdInput input = new() { Id = id, VisibleOwner = currentUser.Owner.VisibleOwner };
        Result<ProjectDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectDto, ProjectResponse>(context);
    }

    public static async Task<IResult> FilterAsync(FilteredProjectRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<FilteredProjectInput, IEnumerable<ProjectDto>> handler,
                                                  CancellationToken cancellationToken)
    {
        FilteredProjectInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<ProjectDto>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectDto, ProjectResponse>(context);
    }

    public static async Task<IResult> GetNamesAsync(GetProjectNamesRequest request,
                                                     HttpContext context,
                                                     ICurrentUser currentUser,
                                                     IHandler<GetProjectNamesInput, IEnumerable<ProjectName>> handler,
                                                     CancellationToken cancellationToken)
    {
        GetProjectNamesInput input = request.ToInput(currentUser.Owner);
        Result<IEnumerable<ProjectName>> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectName, ProjectNameResponse>(context);
    }

    public static async Task<IResult> CreateAsync(CreateProjectRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<CreateProjectInput, ProjectDto> handler,
                                                  CancellationToken cancellationToken)
    {
        CreateProjectInput input = request.ToInput(currentUser.Owner);
        Result<ProjectDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToCreatedResponse<ProjectDto, ProjectResponse>(context, dto => $"/api/projects/{dto.Id}");
    }

    public static async Task<IResult> UpdateAsync(string id,
                                                  UpdateProjectRequest request,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<UpdateProjectInput, ProjectDto> handler,
                                                  CancellationToken cancellationToken)
    {
        UpdateProjectInput input = request.ToInput(id, currentUser.Owner);
        Result<ProjectDto> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToResponse<ProjectDto, ProjectResponse>(context);
    }

    public static async Task<IResult> DeleteAsync(string id,
                                                  HttpContext context,
                                                  ICurrentUser currentUser,
                                                  IHandler<DeleteProjectInput, bool> handler,
                                                  CancellationToken cancellationToken)
    {
        DeleteProjectInput input = new() { Id = id, VisibleOwner = currentUser.Owner.VisibleOwner };
        Result<bool> result = await handler.HandleAsync(input, cancellationToken);

        return result.ToNoContentResponse(context);
    }
}
