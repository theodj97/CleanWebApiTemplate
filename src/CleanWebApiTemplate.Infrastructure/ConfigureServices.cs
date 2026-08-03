using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConnectionStringsSection connectionStrings)
    {
        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionStrings.Sqlite));
        services.AddScoped<ITodoRepository, TodoRepository>();

        return services;
    }
}
