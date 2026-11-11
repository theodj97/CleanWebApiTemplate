using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Common.Composition;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.Create;
using CleanWebApiTemplate.Application.Project.Delete;
using CleanWebApiTemplate.Application.Project.Filter;
using CleanWebApiTemplate.Application.Project.GetById;
using CleanWebApiTemplate.Application.Project.GetNames;
using CleanWebApiTemplate.Application.Project.Update;
using CleanWebApiTemplate.Infrastructure.Project;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>
/// The Project composition module: every registration this feature needs, in one place. Adding a feature is one
/// call in <c>ConfigureServices</c>; removing it is deleting this folder and that call.
/// </summary>
internal static class ProjectModule
{
    public static IServiceCollection AddProjectModule(this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddUseCase<CreateProjectInput, ProjectDto, CreateProjectHandler, CreateProjectInputValidator>();
        services.AddUseCase<GetProjectByIdInput, ProjectDto, GetProjectByIdHandler, GetProjectByIdInputValidator>();
        services.AddUseCase<FilteredProjectInput, IEnumerable<ProjectDto>, FilteredProjectHandler, FilteredProjectInputValidator>();
        services.AddUseCase<UpdateProjectInput, ProjectDto, UpdateProjectHandler, UpdateProjectInputValidator>();
        services.AddUseCase<DeleteProjectInput, bool, DeleteProjectHandler, DeleteProjectInputValidator>();
        services.AddUseCase<GetProjectNamesInput, IEnumerable<ProjectName>, GetProjectNamesHandler, GetProjectNamesInputValidator>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGroupMap, ProjectRoutes>());

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ProjectJsonSerializerContext.Default));

        services.Configure<OpenApiOptions>(OpenApiConfiguration.DocumentName, options => options.AddDocumentTransformer(
            (document, _, _) =>
            {
                ProjectOpenApiSchemas.DescribeSchemas(document);
                return Task.CompletedTask;
            }));

        return services;
    }
}
