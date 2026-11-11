using CleanWebApiTemplate.Api.Common.Abstractions;

namespace CleanWebApiTemplate.Api.Extensions;

public static class EndpointExtension
{
    /// <summary>Maps every route group a feature module registered.</summary>
    public static IApplicationBuilder MapRoutes(this WebApplication app)
    {
        IEnumerable<IGroupMap> endpoints = app.Services.GetRequiredService<IEnumerable<IGroupMap>>();

        foreach (IGroupMap endpoint in endpoints)
            endpoint.MapGroup(app);

        return app;
    }
}
