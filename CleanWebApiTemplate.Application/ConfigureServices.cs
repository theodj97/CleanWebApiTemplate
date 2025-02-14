using CleanWebApiTemplate.Application.Behaviours;
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

        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

        return services;
    }
}
