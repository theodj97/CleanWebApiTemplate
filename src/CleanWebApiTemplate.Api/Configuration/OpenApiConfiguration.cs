using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Shapes the OpenAPI document: a stable title, the resource tag and bearer security. Schema metadata
/// lives in <see cref="CommonOpenApiSchemas"/> and <see cref="TodoOpenApiSchemas"/>. Everything is
/// static, so this stays Native AOT safe: no reflection and no schemas downloaded at runtime.
/// </summary>
internal static class OpenApiConfiguration
{
    private const string TodoTag = "Todo";

    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "CleanWebApiTemplate API";
                document.Info.Version = "1.0.0";
                document.Info.Description =
                    "TODO API built with .NET 10 Minimal APIs, JWT bearer authentication and Native AOT. "
                    + "Owners only see their own TODOs; the Admin and Operator roles unlock broader access.";

                document.Tags = new HashSet<OpenApiTag>
                {
                    new()
                    {
                        Name = TodoTag,
                        Description =
                            "Create, read, filter, update and delete TODO items. Every operation requires a JWT; "
                            + "the ownership and role rules are listed on each operation.",
                    },
                };

                AddBearerSecurity(document);
                CommonOpenApiSchemas.DescribeSchemas(document);
                TodoOpenApiSchemas.DescribeSchemas(document);

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static void AddBearerSecurity(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.TryAdd("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "JWT bearer token. The local explorer mints one from the profile picker; scripts can use "
                + "the `devtoken` tool.",
        });

        document.Security ??= new List<OpenApiSecurityRequirement>();
        if (document.Security.Count == 0)
        {
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        }
    }
}
