using CleanWebApiTemplate.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanWebApiTemplate.Api.UnitTests.ServiceDefaults;

/// <summary>Telemetry export is opt-in through the Aspire-provided endpoint; health endpoints are Development only.</summary>
public class ExtensionsTests
{
    [Fact]
    public void ConfigureOpenTelemetry_WithOtlpEndpoint_RegistersTheExporter()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317";

        // Act
        builder.ConfigureOpenTelemetry();

        // Assert
        Assert.Contains(builder.Services, IsOtlpRegistration);
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithoutOtlpEndpoint_DoesNotRegisterTheExporter()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.ConfigureOpenTelemetry();

        // Assert
        Assert.DoesNotContain(builder.Services, IsOtlpRegistration);
    }

    [Fact]
    public async Task MapDefaultEndpoints_InDevelopment_MapsHealthAndAlive()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.AddDefaultHealthChecks();
        await using var app = builder.Build();

        // Act
        app.MapDefaultEndpoints();

        // Assert
        var routes = MappedRoutes(app);
        Assert.Contains("/health", routes);
        Assert.Contains("/alive", routes);
    }

    [Fact]
    public async Task MapDefaultEndpoints_OutsideDevelopment_MapsNothing()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.AddDefaultHealthChecks();
        await using var app = builder.Build();

        // Act
        app.MapDefaultEndpoints();

        // Assert
        Assert.Empty(MappedRoutes(app));
    }

    private static bool IsOtlpRegistration(ServiceDescriptor descriptor) =>
        ContainsOtlp(descriptor.ServiceType) || ContainsOtlp(descriptor.ImplementationType);

    private static bool ContainsOtlp(Type? type) =>
        type?.FullName?.Contains("Otlp", StringComparison.Ordinal) is true;

    private static string?[] MappedRoutes(WebApplication app) =>
        [.. ((IEndpointRouteBuilder)app).DataSources
                                           .SelectMany(dataSource => dataSource.Endpoints)
                                           .OfType<RouteEndpoint>()
                                           .Select(endpoint => endpoint.RoutePattern.RawText)];
}
