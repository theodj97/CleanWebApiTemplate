using System.Security.Claims;
using System.Text;
using CleanWebApiTemplate.Api.Common;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Identity;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Dtos.Todo;
using CleanWebApiTemplate.Application.Todo.Handlers.Create;
using CleanWebApiTemplate.Application.Todo.Handlers.Delete;
using CleanWebApiTemplate.Application.Todo.Handlers.Filtered;
using CleanWebApiTemplate.Application.Todo.Handlers.GetById;
using CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;
using CleanWebApiTemplate.Application.Todo.Handlers.Update;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories.Todo;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace CleanWebApiTemplate.Api;

/// <summary>
/// Composition root for this executable: every service the API needs is registered here, including
/// the Application and Infrastructure implementations. Layer libraries do not self-register, so each
/// host takes only the pieces it actually uses.
/// </summary>
public static class ConfigureServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
                                                   IConfiguration configuration,
                                                   AppSettings appSettings)
    {
        services.AddPersistence(appSettings.ConnectionStrings);
        services.ConfigureServicesAndValidators();

        var healthChecksBuilder = services.AddHealthChecks()
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
        {
            services.AddEndpointsApiExplorer();
            services.AddApiOpenApi();
        }

        services.AddHttpContextAccessor();
        // Stateless adapter over IHttpContextAccessor: it reads the request per property access. Transient keeps
        // route mapping (outside any request) from capturing a scoped instance.
        services.AddTransient<ICurrentUser, HttpContextCurrentUser>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

        services.RegisterMinimalEndpoints();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services,
                                                     ConnectionStringsSection connectionStrings)
    {
        services.AddSingleton<INpgsqlConnectionFactory>(new NpgsqlConnectionFactory(connectionStrings.Postgres));
        services.AddScoped<ITodoRepository, TodoRepository>();

        return services;
    }

    /// <summary>
    /// Validators and application handlers. Registered one by one on purpose: no assembly scanning,
    /// so every type stays statically visible to the Native AOT analyzer.
    /// </summary>
    private static IServiceCollection ConfigureServicesAndValidators(this IServiceCollection services)
    {
        // Validators
        services.AddTransient<IValidator<CreateTodoInput>, CreateTodoInputValidator>();
        services.AddTransient<IValidator<UpdateTodoInput>, UpdateTodoInputValidator>();
        services.AddTransient<IValidator<DeleteTodoInput>, DeleteTodoInputValidator>();
        services.AddTransient<IValidator<GetTodoByIdInput>, GetTodoByIdInputValidator>();
        services.AddTransient<IValidator<GetTodoTitlesInput>, GetTodoTitlesInputValidator>();
        services.AddTransient<IValidator<FilteredTodoInput>, FilteredTodoInputValidator>();

        // Handlers
        services.AddTransient<IHandler<CreateTodoInput, TodoDto?>, CreateTodoHandler>();
        services.AddTransient<IHandler<UpdateTodoInput, TodoDto?>, UpdateTodoHandler>();
        services.AddTransient<IHandler<DeleteTodoInput, bool>, DeleteTodoHandler>();
        services.AddTransient<IHandler<GetTodoByIdInput, TodoDto?>, GetTodoByIdHandler>();
        services.AddTransient<IHandler<GetTodoTitlesInput, IEnumerable<TodoDto?>>, GetTodoTitlesHandler>();
        services.AddTransient<IHandler<FilteredTodoInput, IEnumerable<TodoDto?>>, FilteredTodoHandler>();

        return services;
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
            opts.AddPolicy(Constants.DEFAULT_CORS_POLICY_NAME, builder =>
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
            .AddPolicy(Constants.ADMIN_POLICY, policy => policy
                .RequireRole(Constants.ADMIN_POLICY)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.OPERATOR_POLICY, policy => policy
                .RequireRole(Constants.OPERATOR_POLICY, Constants.ADMIN_POLICY)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.USER_POLICY, policy => policy
                .RequireRole(Constants.USER_POLICY, Constants.ADMIN_POLICY, Constants.OPERATOR_POLICY)
                .RequireAssertion(HasEmailClaim))
            .AddPolicy(Constants.EXTERNAL_POLICY, policy => policy
                .RequireRole(Constants.EXTERNAL_POLICY, Constants.ADMIN_POLICY, Constants.OPERATOR_POLICY)
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
