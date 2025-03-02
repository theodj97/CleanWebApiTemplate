using CleanWebApiTemplate.Host.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace CleanWebApiTemplate.Host.Routes;

public static class EndpointExtension
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
        .Where(t => typeof(IGroupMap).IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract);

        foreach (var type in endpointTypes)
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IGroupMap), type));

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
