using System.Text;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Application.Todo.Delete;
using CleanWebApiTemplate.Application.Todo.Filtered;
using CleanWebApiTemplate.Application.Todo.GetById;
using CleanWebApiTemplate.Application.Todo.GetTitles;
using CleanWebApiTemplate.Application.Todo.Update;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    /// Validators and application services. Registered one by one on purpose: no assembly scanning,
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

        // Services
        services.AddTransient<ICreateTodoService, CreateTodoService>();
        services.AddTransient<IUpdateTodoService, UpdateTodoService>();
        services.AddTransient<IDeleteTodoService, DeleteTodoService>();
        services.AddTransient<IGetTodoByIdService, GetTodoByIdService>();
        services.AddTransient<IGetTodoTitlesService, GetTodoTitlesService>();
        services.AddTransient<IFilteredTodoService, FilteredTodoService>();

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // No default authority: an empty value is the explicit "no identity provider" mode used
                // locally and by the tests, where tokens are validated with the signing key only.
                // RequireHttpsMetadata keeps its secure default (true): metadata over plain HTTP is never allowed.
                var authority = configuration[Constants.AUTHORITY];
                if (string.IsNullOrWhiteSpace(authority) is false)
                {
                    if (Uri.TryCreate(authority, UriKind.Absolute, out _) is false)
                        throw new InvalidOperationException($"'{Constants.AUTHORITY}' must be an absolute URI.");

                    options.Authority = authority;
                }

                options.Audience = appSettings.Audience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuers = appSettings.ValidIssuers,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        configuration[Constants.API_KEY]
                        ?? throw new InvalidOperationException($"'{Constants.API_KEY}' is required.")))
                };
            });

        return services;
    }
}
