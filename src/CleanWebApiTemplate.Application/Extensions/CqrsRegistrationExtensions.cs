using CleanWebApiTemplate.Application.Abstractions.Messages;
using CleanWebApiTemplate.Application.Decorators;
using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Application.Extensions;

/// <summary>
/// Explicit (no reflection, no assembly scanning, Native AOT friendly) registration
/// of CQRS handlers and their cross-cutting decorators.
/// </summary>
public static class CqrsRegistrationExtensions
{
    /// <summary>
    /// Registers <typeparamref name="THandler"/> as the <see cref="ICommandHandler{TCommand, TResult}"/>
    /// for <typeparamref name="TCommand"/>, wrapped in the <see cref="ValidationCommandHandler{TCommand, TResult}"/> decorator.
    /// If a command must skip validation, register it directly instead:
    /// <c>services.AddTransient&lt;ICommandHandler&lt;TCommand, TResult&gt;, THandler&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, TResult, THandler>(this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where TResult : IResultFactory<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.AddTransient<THandler>();

        services.AddTransient<ICommandHandler<TCommand, TResult>>(sp =>
            new ValidationCommandHandler<TCommand, TResult>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<IEnumerable<IValidator<TCommand>>>()));

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler"/> as the <see cref="IQueryHandler{TQuery, TResult}"/>
    /// for <typeparamref name="TQuery"/>, wrapped in the <see cref="ValidationQueryHandler{TQuery, TResult}"/> decorator.
    /// If a query must skip validation, register it directly instead:
    /// <c>services.AddTransient&lt;IQueryHandler&lt;TQuery, TResult&gt;, THandler&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddQueryHandler<TQuery, TResult, THandler>(this IServiceCollection services)
        where TQuery : IQuery<TResult>
        where TResult : IResultFactory<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.AddTransient<THandler>();

        services.AddTransient<IQueryHandler<TQuery, TResult>>(sp =>
            new ValidationQueryHandler<TQuery, TResult>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<IEnumerable<IValidator<TQuery>>>()));

        return services;
    }
}
