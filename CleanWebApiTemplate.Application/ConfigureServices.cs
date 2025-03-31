using CleanWebApiTemplate.Application.Behaviours;
using CleanWebApiTemplate.Application.Services.Todo;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanWebApiTemplate.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly))
                .AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient)
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

        services.AddCustomServices();

        return services;
    }

    private static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddTransient<ITodoServices, TodoServices>();
        return services;
    }
}
