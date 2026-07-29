using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.CompiledModels;
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
                   // Compiled model: required for trimming/Native AOT (no runtime model building via reflection).
                   // Regenerate after changing the model: dotnet ef dbcontext optimize
                   //   --project src/CleanWebApiTemplate.Infrastructure --startup-project src/CleanWebApiTemplate.Host
                   //   --output-dir CompiledModels --namespace CleanWebApiTemplate.Infrastructure.CompiledModels
                   // options.UseModel(SqlDbContextModel.Instance)
                   options.UseSqlite(connectionStrings.Sqlite,
                       b => b.MigrationsAssembly(assembly)
                   ));

        return services;
    }
}
