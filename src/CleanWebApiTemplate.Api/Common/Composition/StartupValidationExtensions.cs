using CleanWebApiTemplate.Api.Common.Abstractions;

namespace CleanWebApiTemplate.Api.Common.Composition;

/// <summary>Runs the startup checks every composition module registered.</summary>
internal static class StartupValidationExtensions
{
    /// <summary>
    /// Builds every registered use case once. A handler without a validator, a missing registration or a
    /// constructor that cannot be satisfied stops the application here, not on the first request.
    /// </summary>
    public static void ValidateUseCases(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        foreach (IStartupCheck check in scope.ServiceProvider.GetServices<IStartupCheck>())
            check.Run(scope.ServiceProvider);
    }
}
