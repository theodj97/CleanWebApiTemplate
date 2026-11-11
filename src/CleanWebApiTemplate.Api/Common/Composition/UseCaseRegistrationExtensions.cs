using System.Diagnostics.CodeAnalysis;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanWebApiTemplate.Api.Common.Composition;

/// <summary>
/// Registers a use case and everything it needs: its validator, the <see cref="ValidatorSet{TInput}"/> that
/// collects it and the handler. One call per use case, so the three registrations cannot drift apart.
/// </summary>
internal static class UseCaseRegistrationExtensions
{
    public static IServiceCollection AddUseCase<TInput, TResult, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(this IServiceCollection services)
        where TInput : class
        where THandler : class, IHandler<TInput, TResult>
        where TValidator : class, IValidator<TInput>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IValidator<TInput>, TValidator>();
        services.AddTransient<ValidatorSet<TInput>>();
        services.AddTransient<IHandler<TInput, TResult>, THandler>();

        // Resolved once at startup so a use case without validators stops the application instead of
        // failing on its first request.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupCheck, UseCaseStartupCheck<TInput, TResult>>());

        return services;
    }
}

/// <summary>Work the composition root runs once, after the container is built and before the first request.</summary>
internal interface IStartupCheck
{
    void Run(IServiceProvider services);
}

/// <summary>Builds the validator set and the handler, which is what makes a missing registration fail at startup.</summary>
internal sealed class UseCaseStartupCheck<TInput, TResult> : IStartupCheck where TInput : class
{
    public void Run(IServiceProvider services)
    {
        _ = services.GetRequiredService<ValidatorSet<TInput>>();
        _ = services.GetRequiredService<IHandler<TInput, TResult>>();
    }
}
