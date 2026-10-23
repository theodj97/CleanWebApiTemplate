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
                                                   AppSettings appSettings,
                                                   string environment)
    {
        services.AddPersistence(appSettings.ConnectionStrings);
        services.AddApplication();

        var healthChecksBuilder = services.AddHealthChecks()
                .AddCheck("api-health-check", () => HealthCheckResult.Healthy("API is up and running"), tags: ["api"]);
        healthChecksBuilder.AddNpgSql(
            appSettings.ConnectionStrings.Postgres,
            name: "postgres-check",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["postgresDb", "sql"]);

        services.ConfigureCors(environment, appSettings.CorsAllow);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.ConfigureResponseCompression();

        services.ConfigureAuth(environment, appSettings.ValidIssuers, configuration);

        if (environment is not Constants.PRODUCTION_ENVIRONMENT)
        {
            services.AddEndpointsApiExplorer();
            services.AddOpenApi();
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
    private static IServiceCollection AddApplication(this IServiceCollection services)
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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = configuration[Constants.AUTHORITY] ?? "http://localhost:7210";
                if (authority.Length > 0)
                {
                    var authorityUri = new Uri(authority, UriKind.Absolute);

                    options.Authority = authority;
                    // HTTPS stays mandatory for real authorities; the local development
                    // setup clears it and validates with the locally signed key only.
                    options.RequireHttpsMetadata = authorityUri.Scheme == Uri.UriSchemeHttps;
                }

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
