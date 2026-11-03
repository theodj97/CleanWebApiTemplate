using System.Net;

namespace CleanWebApiTemplate.Api.Common.Abstractions;

internal static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder app)
    {
        internal RouteGroupBuilder CreateBaseRouteGroupBuilder(string routeName)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.MapGroup($"/api/{routeName}")
                .WithTags(routeName)
                .ProducesProblem((int)HttpStatusCode.BadRequest)
                .ProducesProblem((int)HttpStatusCode.InternalServerError);
        }

        internal RouteGroupBuilder CreateAuthorizedRouteGroupBuilder(string routeName, string[]? authPolicy = null)
        {
            RouteGroupBuilder routeGroupBuilder = app.CreateBaseRouteGroupBuilder(routeName)
                .ProducesProblem((int)HttpStatusCode.Unauthorized)
                .ProducesProblem((int)HttpStatusCode.Forbidden);

            if (authPolicy is { Length: > 0 })
                routeGroupBuilder.RequireAuthorization(authPolicy);
            else
                routeGroupBuilder.RequireAuthorization();

            return routeGroupBuilder;
        }
    }
}
