using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Common.Composition;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Application.Todo.Delete;
using CleanWebApiTemplate.Application.Todo.Filter;
using CleanWebApiTemplate.Application.Todo.GetById;
using CleanWebApiTemplate.Application.Todo.GetTitles;
using CleanWebApiTemplate.Application.Todo.Update;
using CleanWebApiTemplate.Infrastructure.Todo;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>
/// The Todo composition module: every registration this feature needs, in one place. Adding a feature is one
/// call in <c>ConfigureServices</c>; removing it is deleting this folder and that call.
/// </summary>
internal static class TodoModule
{
    public static IServiceCollection AddTodoModule(this IServiceCollection services)
    {
        services.AddScoped<ITodoRepository, TodoRepository>();

        services.AddUseCase<CreateTodoInput, TodoDto, CreateTodoHandler, CreateTodoInputValidator>();
        services.AddUseCase<GetTodoByIdInput, TodoDto, GetTodoByIdHandler, GetTodoByIdInputValidator>();
        services.AddUseCase<FilteredTodoInput, IEnumerable<TodoDto>, FilteredTodoHandler, FilteredTodoInputValidator>();
        services.AddUseCase<UpdateTodoInput, TodoDto, UpdateTodoHandler, UpdateTodoInputValidator>();
        services.AddUseCase<DeleteTodoInput, bool, DeleteTodoHandler, DeleteTodoInputValidator>();
        services.AddUseCase<GetTodoTitlesInput, IEnumerable<TodoTitle>, GetTodoTitlesHandler, GetTodoTitlesInputValidator>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGroupMap, TodoRoutes>());

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, TodoJsonSerializerContext.Default));

        services.Configure<OpenApiOptions>(OpenApiConfiguration.DocumentName, options => options.AddDocumentTransformer(
            (document, _, _) =>
            {
                TodoOpenApiSchemas.DescribeSchemas(document);
                return Task.CompletedTask;
            }));

        return services;
    }
}
