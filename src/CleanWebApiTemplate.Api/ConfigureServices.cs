using System.Security.Claims;
using System.Text;
using CleanWebApiTemplate.Api.Common;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Todo;
using CleanWebApiTemplate.Application.Common.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace CleanWebApiTemplate.Api;

/// <summary>
/// Composition root for this executable: every service the API needs is registered here or in a feature
/// module. Layer libraries do not self-register, so each host takes only the pieces it actually uses.
/// </summary>
public static class ConfigureServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
                                                   IConfiguration configuration,
                                                   AppSettings appSettings)
    {
        services.AddPostgres(appSettings.ConnectionStrings.Postgres);
        services.AddTodoModule();

        IHealthChecksBuilder healthChecksBuilder = services.AddHealthChecks()
                .AddCheck("api-health-check", () => HealthCheckResult.Healthy("API is up and running"), tags: ["api"]);
        healthChecksBuilder.AddNpgSql(
            appSettings.ConnectionStrings.Postgres,
            name: "postgres-check",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["postgresDb", "sql"]);

        services.ConfigureCors(appSettings.CorsAllowedOrigins, appSettings.CorsAllowCredentials);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.ConfigureResponseCompression();

        services.ConfigureAuth(appSettings, configuration);

        if (appSettings.OpenApiEnabled)
            services.AddApiOpenApi();

        services.AddHttpContextAccessor();
        // Stateless adapter over IHttpContextAccessor: it reads the request per property access. Transient keeps
        // route mapping (outside any request) from capturing a scoped instance.
        services.AddTransient<ICurrentUser, HttpContextCurrentUser>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

        return services;
    }

    /// <summary>
    /// One data source for the process. It owns the connection pool, so repositories open a connection per
    /// operation instead of building a connection string each time.
    /// </summary>
    private static IServiceCollection AddPostgres(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
    }

    /// <summary>
    /// One CORS policy for every host: methods and headers stay open, origins and credentials come from
    /// configuration. An empty origin list allows nobody, which is the local default.
    /// </summary>
    private static IServiceCollection ConfigureCors(this IServiceCollection services,
                                                    string[] allowedOrigins,
                                                    bool allowCredentials)
    {
        services.AddCors(opts =>
            opts.AddPolicy(Constants.DefaultCorsPolicyName, builder =>
            {
                builder.AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithOrigins(allowedOrigins);

                if (allowCredentials)
                    builder.AllowCredentials();
            }));

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
                                                    AppSettings appSettings,
                                                    IConfiguration configuration)
    {
        AuthenticationSettings authentication = AuthenticationSettings.Load(configuration, appSettings);

        services.AddAuthorizationBuilder()
            .AddPolicy(Constants.AdminPolicy, policy => policy
                .RequireRole(Constants.AdminPolicy)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.OperatorPolicy, policy => policy
                .RequireRole(Constants.OperatorPolicy, Constants.AdminPolicy)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.UserPolicy, policy => policy
                .RequireRole(Constants.UserPolicy, Constants.AdminPolicy, Constants.OperatorPolicy)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.ExternalPolicy, policy => policy
                .RequireRole(Constants.ExternalPolicy, Constants.AdminPolicy, Constants.OperatorPolicy)
                .RequireAssertion(HasEmailClaim));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Metadata over plain HTTP is never allowed, even if a misconfigured authority
                // points to http://.
                options.RequireHttpsMetadata = true;
                options.Audience = authentication.Audience;
                options.MapInboundClaims = true;

                if (authentication.Mode is AuthenticationMode.Oidc)
                    options.Authority = authentication.Authority;

                TokenValidationParameters validation = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuers = authentication.ValidIssuers,
                    ValidateIssuerSigningKey = true,
                };

                if (authentication.Mode is AuthenticationMode.Symmetric)
                {
                    validation.IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authentication.ApiKey!));
                    // Pin the algorithm: the local key must only be accepted as HS256.
                    validation.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
                }

                options.TokenValidationParameters = validation;
            });

        return services;
    }

    private static bool HasEmailClaim(AuthorizationHandlerContext context) =>
        context.User.FindFirst(ClaimTypes.Email) is not null
        || context.User.FindFirst("email") is not null;
}
