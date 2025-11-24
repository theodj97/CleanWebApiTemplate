using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConnectionStringsSection connectionStrings)
    {
        var assembly = typeof(ConfigureServices).Assembly;

        services.AddDbContextPool<SqlDbContext>(options =>
            options.UseSqlServer(connectionStrings.SqlServer,
                b => b.MigrationsAssembly(assembly)
            ));

        return services;
    }
}
