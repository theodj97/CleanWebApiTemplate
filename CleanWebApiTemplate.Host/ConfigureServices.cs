using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Configuration;
using CleanWebApiTemplate.Host.Extensions;
using CleanWebApiTemplate.Host.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
// using MongoDB.Driver;
using System.Reflection;
using System.Text;

namespace CleanWebApiTemplate.Host;

public static class ConfigureServices
{
    public static IServiceCollection AddHostServices(this IServiceCollection services,
                                                     IConfiguration configuration,
                                                     string environment,
                                                     string[] corsAllow,
                                                     string[] validIssuers,
                                                     ConnectionStringsSection connectionStrings
                                                     //  MongoUrl mongoDbConnectionStrings
                                                     )
    {
        // services.AddSingleton(new MongoClient(mongoDbConnectionStrings));

        services.AddHealthChecks()
                .AddCheck("api-health-check", () => HealthCheckResult.Healthy("API is up and running"), tags: ["api"])
                .AddSqlServer(
                    connectionString: connectionStrings.SqlServer,
                    name: "sqlserver-check",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["sqlServerDb", "sql"]
                )
                // .AddMongoDb(
                //     name: "mongodb-check",
                //     failureStatus: HealthStatus.Unhealthy,
                //     tags: ["mongoDb", "mongo"]
                // )
                ;

        services.ConfigureCors(environment, corsAllow);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.ConfigureResponseCompression();

        services.ConfigureAuth(environment, validIssuers, configuration);

        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
        {
            services.AddEndpointsApiExplorer();
            services.AddOpenApi();
        }

        services.AddHttpContextAccessor();

        services.AddEndpoints(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection ConfigureCors(this IServiceCollection services,
                                                    string environment,
                                                    string[] corsAllow)
    {
        if (corsAllow is not null && corsAllow.Length != 0)
            services.AddCors(opts =>
            {
                opts.AddPolicy(Constants.DEFAULT_CORS_POLICY_NAME, builder =>
                {
                    builder.AllowAnyMethod()
                           .AllowAnyHeader();

                    if (environment is not Constants.DEV_ENVIRONMNET)
                        builder.WithOrigins(corsAllow)
                               .AllowCredentials();
                });
            });

        return services;
    }

    private static IServiceCollection ConfigureResponseCompression(this IServiceCollection services)
    {
        services.Configure<GzipCompressionProviderOptions>(config => config.Level = System.IO.Compression.CompressionLevel.Fastest);
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
        });

        return services;
    }

    private static IServiceCollection ConfigureAuth(this IServiceCollection services,
                                                    string environment,
                                                    string[] validIssuers,
                                                    IConfiguration configuration)
    {

        services.AddAuthorizationBuilder()
            .AddPolicy(Constants.ADMIN_POLICY, policy => policy.RequireRole(Constants.ADMIN_POLICY))
            .AddPolicy(Constants.OPERATOR_POLICY, policy => policy.RequireRole(Constants.OPERATOR_POLICY,
                                                                               Constants.ADMIN_POLICY))
            .AddPolicy(Constants.USER_POLICY, policy => policy.RequireRole(Constants.USER_POLICY,
                                                                           Constants.ADMIN_POLICY,
                                                                           Constants.OPERATOR_POLICY))
            .AddPolicy(Constants.EXTERNAL_POLICY, policy => policy.RequireRole(Constants.EXTERNAL_POLICY,
                                                                               Constants.ADMIN_POLICY,
                                                                               Constants.OPERATOR_POLICY));

        if (environment is Constants.DEV_ENVIRONMNET)
        {
            services.AddAuthentication(DevAuthHandler.SCHEME_NAME)
                .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SCHEME_NAME, null);

            return services;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "http://localhost:7210";
                options.Audience = "http://localhost:7210";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuers = validIssuers,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[Constants.API_KEY]
                    ?? throw new Exception("No API KEY environmnet variable found")))
                };
            });

        return services;
    }
}

