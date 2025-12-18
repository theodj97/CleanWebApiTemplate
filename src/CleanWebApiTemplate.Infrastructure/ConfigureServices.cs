using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
                                                               ConnectionStringsSection connectionStrings,
                                                               string environment)
    {
        MariaDbServerVersion serverVersion;
        if (environment is Domain.Configuration.Constants.TEST_ENVIRONMENT)
            serverVersion = new MariaDbServerVersion(
                new Version(10, 10, 0)
            );
        else
            serverVersion = new MariaDbServerVersion(
                new Version(12, 1, 0)
            );

        if (environment is Domain.Configuration.Constants.DEV_ENVIRONMNET ||
            environment is Domain.Configuration.Constants.TEST_ENVIRONMENT)
            services.AddDbContextPool<MariaDbContext>(options =>
                options.UseMySql(connectionStrings.MariaDb,
                    serverVersion
                )
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                );
        else
            services.AddDbContextPool<MariaDbContext>(options =>
                options.UseMySql(connectionStrings.MariaDb,
                    serverVersion
                )
            );

        return services;
    }
}
