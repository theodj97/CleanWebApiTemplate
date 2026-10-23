using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace {{ComponentTestsNamespace}};

/// <summary>DI changes allowed in component tests, for scenarios that cannot be provoked any other way.</summary>
public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Removes one hosted service. Never use RemoveAll&lt;IHostedService&gt;(): it also removes the service that starts
    /// the web server, and the test server then fails to start.
    /// </summary>
    public static IServiceCollection RemoveHostedService<THostedService>(this IServiceCollection services)
        where THostedService : class, IHostedService
    {
        var registrations = services
            .Where(d => d.ServiceType == typeof(IHostedService) && !d.IsKeyedService && ImplementationTypeOf(d) == typeof(THostedService))
            .ToList();

        if (registrations.Count == 0)
        {
            throw new InvalidOperationException($"{typeof(THostedService).Name} is not registered as a hosted service.");
        }

        foreach (var registration in registrations)
        {
            services.Remove(registration);
        }

        return services;
    }

    /// <summary>Replaces every registration of <typeparamref name="TService"/> with one instance (fake clock, failing client...).</summary>
    public static IServiceCollection ReplaceWithSingleton<TService>(this IServiceCollection services, TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(instance);
        return services;
    }

    private static Type? ImplementationTypeOf(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType
        ?? descriptor.ImplementationInstance?.GetType()
        // AddHostedService(sp => new X(...)) registers a Func<IServiceProvider, X>: its last generic argument is X.
        ?? descriptor.ImplementationFactory?.GetType().GenericTypeArguments.LastOrDefault();
}
