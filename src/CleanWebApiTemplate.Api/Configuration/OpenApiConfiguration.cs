using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Shapes the document every feature shares: a stable title, bearer security and the common schemas.
/// Everything is static, so this stays Native AOT safe: no reflection and no schemas downloaded at runtime.
/// </summary>
internal static class OpenApiConfiguration
{
    /// <summary>Name of the only document the API serves. Feature modules add their transformers to it.</summary>
    internal const string DocumentName = "v1";

    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options => options.AddDocumentTransformer(
            (document, _, _) =>
            {
                document.Info.Title = "CleanWebApiTemplate API";
                document.Info.Version = "1.0.0";
                document.Info.Description =
                    "API built with .NET 10 Minimal APIs, JWT bearer authentication and Native AOT. "
                    + "Callers only see their own items; the Admin role unlocks every owner.";

                AddBearerSecurity(document);
                CommonOpenApiSchemas.DescribeSchemas(document);

                return Task.CompletedTask;
            }));

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
