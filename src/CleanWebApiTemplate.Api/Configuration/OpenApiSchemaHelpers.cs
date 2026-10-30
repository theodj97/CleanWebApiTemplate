using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>Small, AOT-safe helpers for shaping OpenAPI schemas.</summary>
internal static class OpenApiSchemaHelpers
{
    public static void DescribePaging(OpenApiSchema schema)
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

    public static void DescribeSort(OpenApiSchema? schema)
    {
        if (schema is null)
            return;

        schema.Description =
            "Sort keys and directions, applied in order. Supported keys: id, title, description, "
            + "createdAt, updatedAt, status, createdBy, updatedBy.";
        schema.Example = JsonNode.Parse("""[{"key":"createdAt","value":false}]""");
    }

    public static void DescribeStatus(OpenApiSchema? schema,
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

    public static void Describe(OpenApiSchema? schema,
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

    public static OpenApiSchema? Property(OpenApiSchema schema, string name) =>
        schema.Properties is not null && schema.Properties.TryGetValue(name, out var property)
            ? property as OpenApiSchema
            : null;

    public static OpenApiSchema? ArrayItems(OpenApiSchema? schema) => schema?.Items as OpenApiSchema;

    public static void Patch(IDictionary<string, IOpenApiSchema> schemas,
                             string name,
                             Action<OpenApiSchema> describe)
    {
        if (schemas.TryGetValue(name, out var node) && node is OpenApiSchema schema)
            describe(schema);
    }
}
