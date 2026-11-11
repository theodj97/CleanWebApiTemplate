namespace CleanWebApiTemplate.Api.Common.Abstractions;

/// <summary>Route-group helpers. Every feature group gets the same API prefix, tag and ProblemDetails contract.</summary>
internal static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder app)
    {
        internal RouteGroupBuilder CreateBaseRouteGroupBuilder(string route, string tag)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.MapGroup($"/api/{route}")
                .WithTags(tag)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }

        internal RouteGroupBuilder CreateAuthorizedRouteGroupBuilder(string route,
                                                                      string tag,
                                                                      string[]? authPolicies = null)
        {
            RouteGroupBuilder routeGroupBuilder = app.CreateBaseRouteGroupBuilder(route, tag)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden);

            if (authPolicies is { Length: > 0 })
                routeGroupBuilder.RequireAuthorization(authPolicies);
            else
                routeGroupBuilder.RequireAuthorization();

            return routeGroupBuilder;
        }
    }
}
