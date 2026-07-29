using CleanWebApiTemplate.Host.Models.Interfaces;
using CleanWebApiTemplate.Host.Routes.Todo;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanWebApiTemplate.Host.Extensions;

public static class EndpointExtension
{
    /// <summary>
    /// Explicit (no assembly scanning, Native AOT friendly) registration of route groups.
    /// Register each <see cref="IGroupMap"/> implementation here.
    /// </summary>
    public static IServiceCollection RegisterMinimalEndpoints(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IGroupMap, TodoRoutes>());

        return services;
    }

    public static IApplicationBuilder MapRoutes(this WebApplication app)
    {
        IEnumerable<IGroupMap> endpoints = app.Services.GetRequiredService<IEnumerable<IGroupMap>>();

        foreach (IGroupMap endpoint in endpoints)
            endpoint.MapGroup(app);

        return app;
    }
}
