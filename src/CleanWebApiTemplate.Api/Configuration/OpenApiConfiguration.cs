using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Shapes the OpenAPI document that the local explorer (and any other consumer) reads: a stable
/// title, the resource tag, identifier formats and the field constraints that the validators
/// enforce. The metadata is static, so this stays Native AOT safe: no reflection and no schemas
/// downloaded at runtime.
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
                DescribeSchemas(document);

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

    private static void DescribeSchemas(OpenApiDocument document)
    {
        if (document.Components?.Schemas is not { } schemas)
            return;

        if (schemas.TryGetValue("Ulid", out var ulidNode) && ulidNode is OpenApiSchema ulid)
        {
            ulid.Type = JsonSchemaType.String;
            ulid.Format = "ulid";
            ulid.Pattern = "^[0-9A-HJKMNP-TV-Z]{26}$";
            ulid.Description = "ULID identifier: 26 characters in Crockford Base32.";
            ulid.Example = JsonValue.Create("01HWXHRB80000G000000000001");
        }

        if (schemas.TryGetValue("ProblemDetails", out var problemNode) && problemNode is OpenApiSchema problem)
        {
            problem.Description = "Problem details (RFC 9457) returned for 4xx and 5xx responses.";
            Describe(Property(problem, "type"), "Problem type identifier.");
            Describe(Property(problem, "title"), "Short, human-readable summary of the problem.");
            Describe(Property(problem, "status"), "HTTP status code.");
            Describe(Property(problem, "detail"), "Human-readable explanation of this occurrence.");
            Describe(Property(problem, "instance"), "URI that identifies this specific occurrence.");
        }

        if (schemas.TryGetValue("KeyValuePairOfstringAndboolean", out var sortNode)
            && sortNode is OpenApiSchema sort)
        {
            sort.Description = "One sort key and whether its order is descending.";
            Describe(Property(sort, "key"),
                     "Field name: id, title, description, createdAt, updatedAt, status, createdBy or updatedBy.");
            Describe(Property(sort, "value"), "True for descending order, false for ascending.");
        }

        Patch(schemas, "CreateTodoRequest", schema =>
        {
            Describe(Property(schema, "title"),
                     "Short task description. Trimmed; unique per owner.",
                     maxLength: 255,
                     example: "Prepare the release notes");
            Describe(Property(schema, "description"),
                     "Optional details for the task.",
                     maxLength: 1000,
                     example: "Include the migration notes and the changelog link.");
        });

        Patch(schemas, "UpdateTodoRequest", schema =>
        {
            Describe(Property(schema, "title"),
                     "New title. Titles stay unique per owner.",
                     maxLength: 255,
                     example: "Publish the release notes");
            Describe(Property(schema, "description"),
                     "New details for the task.",
                     maxLength: 1000,
                     example: "The draft is ready for review.");
            DescribeStatus(Property(schema, "status"),
                           "New status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled. Omit to leave it unchanged.",
                           withAllowedValues: false);
        });

        Patch(schemas, "FilteredTodoRequest", schema =>
        {
            Describe(ArrayItems(Property(schema, "ids")),
                     "TODO ids to include.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(ArrayItems(Property(schema, "title")),
                     "Title fragments to match (contains).",
                     maxLength: 255,
                     example: "release");
            DescribeStatus(ArrayItems(Property(schema, "status")), withAllowedValues: true);
            Describe(ArrayItems(Property(schema, "createdBy")),
                     "Owner emails. Admin only; other roles are always scoped to their own email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "startDate"),
                     "Inclusive lower bound for CreatedAt.",
                     format: "date-time",
                     example: "2026-01-01T00:00:00Z");
            Describe(Property(schema, "endDate"),
                     "Inclusive upper bound for CreatedAt.",
                     format: "date-time",
                     example: "2026-12-31T23:59:59Z");

            DescribePaging(schema);
            DescribeSort(Property(schema, "sortProperties"));
        });

        Patch(schemas, "GetTodoTitlesRequest", schema =>
        {
            DescribePaging(schema);
            DescribeSort(Property(schema, "sortProperties"));
        });

        Patch(schemas, "TodoResponse", schema =>
        {
            Describe(Property(schema, "id"),
                     "TODO identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "title"),
                     "Task title.",
                     maxLength: 255,
                     example: "Set up the local environment");
            Describe(Property(schema, "description"),
                     "Task details.",
                     maxLength: 1000,
                     example: "Started with Aspire, PostgreSQL and locally signed JWTs.");
            Describe(Property(schema, "createdAt"),
                     "Creation timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T09:15:00Z");
            DescribeStatus(Property(schema, "status"), withAllowedValues: true);
            Describe(Property(schema, "createdBy"),
                     "Owner email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "updatedBy"),
                     "Email of the last writer.",
                     format: "email",
                     example: "local.user@example.com");
        });

        Patch(schemas, "TodoTitleResponse", schema =>
        {
            Describe(Property(schema, "id"),
                     "TODO identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "title"),
                     "Task title.",
                     maxLength: 255,
                     example: "Set up the local environment");
        });
    }

    private static void DescribePaging(OpenApiSchema schema)
    {
        var pageNumber = Property(schema, "pageNumber");
        if (pageNumber is not null)
        {
            pageNumber.Description = "1-based page number. Required when pageSize is set.";
            pageNumber.Minimum = "1";
            pageNumber.Example = JsonValue.Create(1);
        }

        var pageSize = Property(schema, "pageSize");
        if (pageSize is not null)
        {
            pageSize.Description = "Page size. Required when pageNumber is set.";
            pageSize.Minimum = "1";
            pageSize.Example = JsonValue.Create(10);
        }
    }

    private static void DescribeSort(OpenApiSchema? schema)
    {
        if (schema is null)
            return;

        schema.Description =
            "Sort keys and directions, applied in order. Supported keys: id, title, description, "
            + "createdAt, updatedAt, status, createdBy, updatedBy.";
        schema.Example = JsonNode.Parse("""[{"key":"createdAt","value":false}]""");
    }

    private static void DescribeStatus(OpenApiSchema? schema,
                                       string? description = null,
                                       bool withAllowedValues = true)
    {
        if (schema is null)
            return;

        schema.Description = description ?? "Status: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.";
        schema.Example = JsonValue.Create(1);

        if (!withAllowedValues)
            return;

        schema.Enum =
        [
            JsonValue.Create(1),
            JsonValue.Create(2),
            JsonValue.Create(3),
            JsonValue.Create(4),
        ];
    }

    private static void Describe(OpenApiSchema? schema,
                                 string description,
                                 string? format = null,
                                 int? maxLength = null,
                                 string? example = null)
    {
        if (schema is null)
            return;

        schema.Description = description;

        if (format is not null)
            schema.Format = format;

        if (maxLength is not null)
            schema.MaxLength = maxLength;

        if (example is not null)
            schema.Example = JsonValue.Create(example);
    }

    private static OpenApiSchema? Property(OpenApiSchema schema, string name) =>
        schema.Properties is not null && schema.Properties.TryGetValue(name, out var property)
            ? property as OpenApiSchema
            : null;

    private static OpenApiSchema? ArrayItems(OpenApiSchema? schema) => schema?.Items as OpenApiSchema;

    private static void Patch(IDictionary<string, IOpenApiSchema> schemas,
                              string name,
                              Action<OpenApiSchema> describe)
    {
        if (schemas.TryGetValue(name, out var node) && node is OpenApiSchema schema)
            describe(schema);
    }
}
