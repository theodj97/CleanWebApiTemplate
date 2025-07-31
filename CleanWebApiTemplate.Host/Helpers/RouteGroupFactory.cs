using System.Net;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace CleanWebApiTemplate.Host.Helpers;

public static class RouteGroupFactory
{
    /// <summary>
    /// Create a group of api routes with authorization, and fluentValidationFilter.
    /// </summary>
    /// <param name="app">Extension</param>
    /// <param name="routeName">The name of the route</param>
    /// <param name="authPolicies">Auth policies to apply</param>
    /// <param name="authSchemas">Auth schemas to apply</param>
    /// <param name="excludeFromSwagger">Exclude this group from Swagger or not</param>
    /// <returns></returns>
    public static RouteGroupBuilder CreateGroup(this IEndpointRouteBuilder app,
                                                string routeName,
                                                string[]? authPolicy = null,
                                                string[]? authSchemas = null,
                                                bool excludeFromSwagger = false)
    {
        RouteGroupBuilder routeGroupBuilder = app.MapGroup($"/api/{routeName}")
            .WithTags(routeName)
            .AddFluentValidationFilter()
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .ProducesProblem((int)HttpStatusCode.Unauthorized);

        if (excludeFromSwagger)
            routeGroupBuilder.WithMetadata(new ExcludeFromDescriptionAttribute());

        bool isAuthPolicySet = false;

        if (authPolicy is not null && authPolicy.Length != 0)
        {
            if (authPolicy.Where(policy => !Constants.AuthorizationPolicies.Contains(policy)).Any())
                throw new ArgumentException($"Invalid authPolicy in policies: {string.Join(", ", authPolicy)}.");

            routeGroupBuilder.RequireAuthorization(authPolicy);
            isAuthPolicySet = true;
        }

        if (authSchemas is not null && authSchemas.Length != 0)
        {
            var invalidSchemas = authSchemas.Where(schema => !Constants.AuthenticationPolicies.Contains(schema)).ToArray();
            if (invalidSchemas.Length != 0)
                throw new ArgumentException($"Invalid : {string.Join(", ", invalidSchemas)}.");

            var attribute = new AuthorizeAttribute
            {
                AuthenticationSchemes = string.Join(",", authSchemas)
            };

            routeGroupBuilder.RequireAuthorization(attribute);
            isAuthPolicySet = true;
        }

        if (isAuthPolicySet is false)
            routeGroupBuilder.RequireAuthorization();

        return routeGroupBuilder;
    }
}
