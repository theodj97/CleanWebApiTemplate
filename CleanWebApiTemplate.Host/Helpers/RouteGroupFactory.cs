using CleanWebApiTemplate.Domain.Configuration;

namespace CleanWebApiTemplate.Host.Helpers;

public static class RouteGroupFactory
{
    /// <summary>
    /// Create a group of api routes with authorization, and fluentValidationFilter.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="routeName"></param>
    /// <param name="authPolicy"></param>
    /// <returns></returns>
    public static RouteGroupBuilder CreateAuthorizedGroup(this IEndpointRouteBuilder app, string routeName, string[]? authPolicy = null)
    {
        var routeGroupBuilder = app.MapGroup($"/api/{routeName}")
            .WithTags(routeName)
            .AddFluentValidationFilter();

        if (authPolicy is not null)
        {
            if (authPolicy.Where(policy => !Constants.AUTHORIZATION_POLICIES.Contains(policy)).Any())
                throw new ArgumentException($"Invalid authPolicy in policies: {string.Join(", ", authPolicy)}.");

            routeGroupBuilder.RequireAuthorization(authPolicy);
        }
        else
            routeGroupBuilder.RequireAuthorization();

        return routeGroupBuilder;
    }
}
