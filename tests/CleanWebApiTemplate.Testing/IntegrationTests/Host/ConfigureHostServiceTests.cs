using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host;
using CleanWebApiTemplate.Host.Configuration;
using CleanWebApiTemplate.Testing.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if (ApiHealthCheck)
using Microsoft.Extensions.Diagnostics.HealthChecks;
#endif
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace CleanWebApiTemplate.Testing.IntegrationTests.Host;

public class ConfigureHostServiceTests
{
    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.INTEGRATION)]
    public void AddHostServices_DevEnvironment_RegistersCoreServices()
    {
        // Arrange 
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        string[]? corsAllow = ["test.com"];
        string[]? validIssuers = ["localhost"];
        ConnectionStringsSection sqliteConnectionString = new()
        {
#if (IsSQLite)
            Sqlite = "Data Source=TestDb.db"
#endif
        };

        // Act
        services = ConfigureServices.AddHostServices(services,
                                                     config,
                                                     Constants.DEV_ENVIRONMNET,
                                                     corsAllow,
                                                     validIssuers,
                                                     sqliteConnectionString);

        // Assert
#if (ApiHealthCheck)
        var healthCheckService = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(HealthCheckService));
        Assert.NotNull(healthCheckService);
        Assert.Equal(ServiceLifetime.Singleton, healthCheckService.Lifetime);
#endif

        var corsService = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(ICorsService));
        Assert.NotNull(corsService);
        Assert.Equal(ServiceLifetime.Transient, corsService.Lifetime);

        var exceptionHandler = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IExceptionHandler));
        Assert.NotNull(exceptionHandler);
        Assert.Equal(typeof(GlobalExceptionHandler), exceptionHandler.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, exceptionHandler.Lifetime);

        var problemDetails = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IProblemDetailsWriter));
        Assert.NotNull(problemDetails);
        Assert.Equal(ServiceLifetime.Singleton, problemDetails.Lifetime);

        var gzipOptions = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IConfigureOptions<GzipCompressionProviderOptions>));
        Assert.NotNull(gzipOptions);

        var serviceProvider = services.BuildServiceProvider();
        var responseCompressionOptions = serviceProvider.GetRequiredService<IOptions<ResponseCompressionOptions>>().Value;
        Assert.True(responseCompressionOptions.EnableForHttps);
        Assert.Contains("application/json", responseCompressionOptions.MimeTypes);

        var gzipConfig = serviceProvider.GetRequiredService<IOptions<GzipCompressionProviderOptions>>().Value;
        Assert.Equal(CompressionLevel.Fastest, gzipConfig.Level);

        var authSchemeProvider = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IAuthenticationHandlerProvider));
        Assert.NotNull(authSchemeProvider);
        Assert.Equal(ServiceLifetime.Scoped, authSchemeProvider.Lifetime);

        var apiExplorer = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IApiDescriptionGroupCollectionProvider));
        Assert.NotNull(apiExplorer);

        var httpContextAccessor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IHttpContextAccessor));
        Assert.NotNull(httpContextAccessor);
        Assert.Equal(typeof(HttpContextAccessor), httpContextAccessor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, httpContextAccessor.Lifetime);

        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(Constants.DEFAULT_CORS_POLICY_NAME);
        Assert.NotNull(policy);
        Assert.DoesNotContain(corsAllow[0], policy.Origins);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.INTEGRATION)]
    public void AddHostServices_TestEnvironment_RegistersCoreServices()
    {
        // Arrange 
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        string[]? corsAllow = ["test.com"];
        string[]? validIssuers = ["localhost"];
        ConnectionStringsSection sqliteConnectionString = new()
        {
#if (IsSQLite)
            Sqlite = "Data Source=TestDb.db"
#endif
        };

        // Act
        services = ConfigureServices.AddHostServices(services,
                                                     config,
                                                     Constants.TEST_ENVIRONMENT,
                                                     corsAllow,
                                                     validIssuers,
                                                     sqliteConnectionString);

        // Assert
#if (ApiHealthCheck)
        var healthCheckService = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(HealthCheckService));
        Assert.NotNull(healthCheckService);
        Assert.Equal(ServiceLifetime.Singleton, healthCheckService.Lifetime);
#endif

        var corsService = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(ICorsService));
        Assert.NotNull(corsService);
        Assert.Equal(ServiceLifetime.Transient, corsService.Lifetime);

        var exceptionHandler = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IExceptionHandler));
        Assert.NotNull(exceptionHandler);
        Assert.Equal(typeof(GlobalExceptionHandler), exceptionHandler.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, exceptionHandler.Lifetime);

        var problemDetails = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IProblemDetailsWriter));
        Assert.NotNull(problemDetails);
        Assert.Equal(ServiceLifetime.Singleton, problemDetails.Lifetime);

        var gzipOptions = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IConfigureOptions<GzipCompressionProviderOptions>));
        Assert.NotNull(gzipOptions);

        var serviceProvider = services.BuildServiceProvider();
        var responseCompressionOptions = serviceProvider.GetRequiredService<IOptions<ResponseCompressionOptions>>().Value;
        Assert.True(responseCompressionOptions.EnableForHttps);
        Assert.Contains("application/json", responseCompressionOptions.MimeTypes);

        var gzipConfig = serviceProvider.GetRequiredService<IOptions<GzipCompressionProviderOptions>>().Value;
        Assert.Equal(CompressionLevel.Fastest, gzipConfig.Level);

        var authSchemeProvider = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IAuthenticationHandlerProvider));
        Assert.NotNull(authSchemeProvider);
        Assert.Equal(ServiceLifetime.Scoped, authSchemeProvider.Lifetime);

        var apiExplorer = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IApiDescriptionGroupCollectionProvider));
        Assert.NotNull(apiExplorer);

        var httpContextAccessor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IHttpContextAccessor));
        Assert.NotNull(httpContextAccessor);
        Assert.Equal(typeof(HttpContextAccessor), httpContextAccessor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, httpContextAccessor.Lifetime);

        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(Constants.DEFAULT_CORS_POLICY_NAME);
        Assert.NotNull(policy);
        Assert.Contains(corsAllow[0], policy.Origins);
    }
}
