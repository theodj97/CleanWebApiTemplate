using CleanWebApiTemplate.Application.Behaviours;
using FluentValidation;
using CustomMediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanWebApiTemplate.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(typeof(ConfigureServices).Assembly)
                .AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient)
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

        return services;
    }
}
