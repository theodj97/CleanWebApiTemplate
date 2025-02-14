using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Infrastructure.Helpers;
using CleanWebApiTemplate.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection ConfigureInfrastructureServices(this IServiceCollection services,
                                                                     string sqlServerCnnStrings,
                                                                     string mongoDbCnnStrings,
                                                                     string mongoDbName)
    {
        services.AddDbContextPool<SqlDbContext>(options =>
            options.UseSqlServer(sqlServerCnnStrings,
            b => b.MigrationsAssembly(Assembly.GetExecutingAssembly())
            ));

        services.AddDbContextPool<MongoDbContext>(options =>
            options.UseMongoDB(mongoDbCnnStrings, mongoDbName));

        services.AddTransient(typeof(IBaseRepository<>), typeof(BaseSqlRepository<>));
        services.AddTransient(typeof(IBaseRepository<>), typeof(BaseMongoRepository<>));

        return services;
    }
}
