using System.Text.Json.Nodes;
using CleanWebApiTemplate.Application.Todo.Sorting;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
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
            $"Sort keys and directions, applied in order. Supported keys: {TodoSortCatalog.Describe()}.";
        schema.Example = JsonNode.Parse("""[{"key":"createdAt","value":false}]""");
    }

    public static void DescribeStatus(OpenApiSchema? schema,
                                      string? description = null,
                                      bool withAllowedValues = true)
    {
        if (schema is null)
            return;

        string values = string.Join(", ", Enum.GetValues<ETodoStatus>()
                                                    .Select(status => $"{(int)status} {status}"));
        schema.Description = description is null ? $"Status: {values}." : $"{description} Values: {values}.";
        schema.Example = JsonValue.Create((int)ETodoStatus.Pending);

        if (!withAllowedValues)
            return;

        schema.Enum = [.. Enum.GetValues<ETodoStatus>().Select(status => JsonValue.Create((int)status))];
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
