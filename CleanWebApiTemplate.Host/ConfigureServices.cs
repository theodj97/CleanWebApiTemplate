using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CleanWebApiTemplate.Host;

public static class ConfigureServices
{
    public static IServiceCollection AddHostServices(this IServiceCollection services,
                                                     IConfiguration configuration,
                                                     string environment,
                                                     string[]? corsAllow,
                                                     string[]? validIssuers,
                                                     string sqlConnectionStrings)
    {
        services.AddHealthChecks()
            .AddCheck("api-health-check", () => HealthCheckResult.Healthy("API is up and running"), tags: ["api"])
            .AddSqlServer(
                connectionString: sqlConnectionStrings,
                name: "sqlserver-check",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database"]
            );


        services.ConfigureCors(environment, corsAllow);

        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
            services.AddEndpointsApiExplorer();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.ConfigureResponseCompression();

        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
            services.AddOpenApi();

        services.ConfigureAuth(environment, validIssuers, configuration);

        //services.AddGrpc();

        services.AddHttpContextAccessor();

        return services;
    }

    private static IServiceCollection ConfigureCors(this IServiceCollection services,
                                                    string environment,
                                                    string[]? corsAllow)
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
                                                    string[]? validIssuers,
                                                    IConfiguration configuration)
    {
        if (environment is Constants.DEV_ENVIRONMNET)
            return services;

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

        services.AddAuthorizationBuilder()
            .AddPolicy(Constants.ADMIN_POLICY, policy => policy.RequireRole("Admin"))
            .AddPolicy(Constants.OPERATOR_POLICY, policy => policy.RequireRole("Operator"))
            .AddPolicy(Constants.USER_POLICY, policy => policy.RequireRole("User"))
            .AddPolicy(Constants.EXTERNAL_POLICY, policy => policy.RequireRole("External"));

        return services;
    }
}

