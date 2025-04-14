using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// using MongoDB.Driver;

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

        // services.AddDbContextPool<MongoDbContext>(options =>
        //     options.UseMongoDB(mongoDbCnnStrings.ToString(), mongoDbCnnStrings.DatabaseName));

        services.AddTransient(typeof(IBaseRepository<>), typeof(BaseSqlRepository<>));
        // services.AddTransient(typeof(IBaseRepository<>), typeof(BaseMongoRepository<>));

        return services;
    }
}
