using System.Net;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Common.Abstractions;

/// <summary>The route-group helpers carry the prefix, tag, ProblemDetails and auth metadata.</summary>
public class EndpointRouteBuilderExtensionsTests
{
    [Fact]
    public void CreateBaseRouteGroupBuilder_MapsUnderApiAndDocumentsClientAndServerErrors()
    {
        // Arrange
        using var app = CreateApp();

        // Act
        var group = app.CreateBaseRouteGroupBuilder("Public");
        group.MapGet("/", () => "ok");

        // Assert
        var endpoint = GetSingleEndpoint(app);
        Assert.StartsWith("/api/Public", ((RouteEndpoint)endpoint).RoutePattern.RawText, StringComparison.Ordinal);
        Assert.Contains("Public", GetTags(endpoint));
        Assert.Contains(GetProblemStatuses(endpoint), status => status == (int)HttpStatusCode.BadRequest);
        Assert.Contains(GetProblemStatuses(endpoint), status => status == (int)HttpStatusCode.InternalServerError);
        Assert.Null(endpoint.Metadata.GetMetadata<AuthorizeAttribute>());
    }

    [Fact]
    public void CreateAuthorizedRouteGroupBuilder_WithPolicy_AddsPolicyAndAuthProblemMetadata()
    {
        // Arrange
        using var app = CreateApp();

        // Act
        var group = app.CreateAuthorizedRouteGroupBuilder("Todo", [Constants.USER_POLICY]);
        group.MapGet("/", () => "ok");

        // Assert
        var endpoint = GetSingleEndpoint(app);
        var authorize = endpoint.Metadata.GetMetadata<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(Constants.USER_POLICY, authorize.Policy);
        Assert.Contains(GetProblemStatuses(endpoint), status => status == (int)HttpStatusCode.Unauthorized);
        Assert.Contains(GetProblemStatuses(endpoint), status => status == (int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public void CreateAuthorizedRouteGroupBuilder_WithoutPolicy_RequiresAuthentication()
    {
        // Arrange
        using var app = CreateApp();

        // Act
        var group = app.CreateAuthorizedRouteGroupBuilder("Authenticated");
        group.MapGet("/", () => "ok");

        // Assert
        var endpoint = GetSingleEndpoint(app);
        var authorize = endpoint.Metadata.GetMetadata<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Null(authorize.Policy);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
        });
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();

        return builder.Build();
    }

    private static Endpoint GetSingleEndpoint(WebApplication app) =>
        Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));

    private static IEnumerable<string> GetTags(Endpoint endpoint) =>
        endpoint.Metadata.OfType<TagsAttribute>().SelectMany(attribute => attribute.Tags);

    private static IEnumerable<int> GetProblemStatuses(Endpoint endpoint) =>
        endpoint.Metadata.OfType<ProducesResponseTypeMetadata>().Select(metadata => metadata.StatusCode);
}
