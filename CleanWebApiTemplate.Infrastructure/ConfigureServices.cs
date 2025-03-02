using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Reflection;

namespace CleanWebApiTemplate.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
                                                               string sqlServerCnnStrings,
                                                               MongoUrl mongoDbCnnStrings)
    {
        services.AddDbContextPool<SqlDbContext>(options =>
            options.UseSqlServer(sqlServerCnnStrings,
            b => b.MigrationsAssembly(Assembly.GetExecutingAssembly())
            ));

        services.AddDbContextPool<MongoDbContext>(options =>
            options.UseMongoDB(mongoDbCnnStrings.ToString(), mongoDbCnnStrings.DatabaseName));

        services.AddTransient(typeof(BaseSqlRepository<>));
        services.AddTransient(typeof(BaseMongoRepository<>));

        services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepositoryDispatcher<>));

        return services;
    }
}
