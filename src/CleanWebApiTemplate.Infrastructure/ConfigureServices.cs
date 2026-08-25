using CleanWebApiTemplate.Domain.Configuration;
#if (IsSQLite)
using CleanWebApiTemplate.Infrastructure.Data;
#endif
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConnectionStringsSection connectionStrings)
    {
#if (IsSQLite)
        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionStrings.Sqlite));
        services.AddScoped<ITodoRepository, TodoRepository>();
#endif

        return services;
    }
}
