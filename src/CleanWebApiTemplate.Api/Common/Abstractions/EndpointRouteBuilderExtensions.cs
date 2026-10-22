using System.Net;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Common.Abstractions;

internal static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder app)
    {
        internal RouteGroupBuilder MapAuthorizedGroup(string routeName,
                                                      string[]? authPolicy = null,
                                                      bool addOpenApiMetadata = true,
                                                      OpenApiParameter[]? openApiParameters = null)
        {
            ArgumentNullException.ThrowIfNull(app);

            RouteGroupBuilder routeGroupBuilder = app.CreateBaseRouteGroupBuilder(routeName,
                                                                                  addOpenApiMetadata,
                                                                                  openApiParameters);

            if (addOpenApiMetadata)
                routeGroupBuilder.ProducesProblem(
                    (int)HttpStatusCode.Unauthorized);

            if (authPolicy is { Length: > 0 })
                routeGroupBuilder.RequireAuthorization(authPolicy);

            else
                routeGroupBuilder.RequireAuthorization();

            return routeGroupBuilder;
        }

        internal RouteGroupBuilder CreateBaseRouteGroupBuilder(string routeName,
                                                               bool addOpenApiMetadata = true,
                                                               OpenApiParameter[]? openApiParameters = null)
        {
            RouteGroupBuilder routeGroupBuilder = app.MapGroup($"/api/{routeName}")
                .WithTags(routeName);

            if (addOpenApiMetadata is false)
                routeGroupBuilder.WithMetadata(new ExcludeFromDescriptionAttribute());
            else
            {
                routeGroupBuilder.ProducesProblem((int)HttpStatusCode.InternalServerError);

                if (addOpenApiMetadata && openApiParameters is not null && openApiParameters.Length != 0)
                    routeGroupBuilder.AddOpenApiOperationTransformer((operation, context, ct) =>
                    {
                        operation.Parameters ??= [];
                        foreach (var parameter in openApiParameters)
                            operation.Parameters.Add(parameter);

                        return Task.CompletedTask;
                    });
            }

            return routeGroupBuilder;
        }

        internal RouteGroupBuilder CreateAuthorizedRouteGroupBuilder(string routeName,
                                                                     string[]? authPolicy = null,
                                                                     bool addOpenApiMetadata = true,
                                                                     OpenApiParameter[]? openApiParameters = null)
        {
            RouteGroupBuilder routeGroupBuilder = app.CreateBaseRouteGroupBuilder(routeName, addOpenApiMetadata, openApiParameters);

            if (addOpenApiMetadata is true)
                routeGroupBuilder.ProducesProblem((int)HttpStatusCode.Unauthorized);

            if (authPolicy is not null && authPolicy.Length != 0)
                routeGroupBuilder.RequireAuthorization(authPolicy);
            else
                routeGroupBuilder.RequireAuthorization();

            return routeGroupBuilder;
        }
    }
}
