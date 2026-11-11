using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>Small, AOT-safe helpers for shaping OpenAPI schemas.</summary>
internal static class OpenApiSchemaHelpers
{
    public static void DescribePaging(OpenApiSchema schema)
    {
        OpenApiSchema? pageNumber = Property(schema, "pageNumber");
        if (pageNumber is not null)
        {
            pageNumber.Description = "1-based page number. Required when pageSize is set.";
            pageNumber.Minimum = "1";
            pageNumber.Example = JsonValue.Create(1);
        }

        OpenApiSchema? pageSize = Property(schema, "pageSize");
        if (pageSize is not null)
        {
            pageSize.Description = "Page size. Required when pageNumber is set.";
            pageSize.Minimum = "1";
            pageSize.Example = JsonValue.Create(10);
        }
    }

    /// <summary>Describes a sort collection with the keys the feature accepts.</summary>
    public static void DescribeSort(OpenApiSchema? schema, string keys)
    {
        if (schema is null)
            return;

        schema.Description = $"Sort keys and directions, applied in order. Supported keys: {keys}.";
        schema.Example = JsonNode.Parse("""[{"key":"createdAt","value":false}]""");
    }

    /// <summary>Describes a status property from its enum: names, allowed values and an example.</summary>
    public static void DescribeStatus<TEnum>(OpenApiSchema? schema,
                                             string? description = null,
                                             bool withAllowedValues = true)
        where TEnum : struct, Enum
    {
        if (schema is null)
            return;

        TEnum[] values = [.. Enum.GetValues<TEnum>()];
        string names = string.Join(", ", values.Select(status => $"{Convert.ToInt32(status, CultureInfo.InvariantCulture)} {status}"));
        schema.Description = description is null ? $"Status: {names}." : $"{description} Values: {names}.";
        schema.Example = JsonValue.Create(Convert.ToInt32(values[0], CultureInfo.InvariantCulture));

        if (withAllowedValues is false)
            return;

        schema.Enum = [.. values.Select(status => JsonValue.Create(Convert.ToInt32(status, CultureInfo.InvariantCulture)))];
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
        schema.Properties is not null && schema.Properties.TryGetValue(name, out IOpenApiSchema? property)
            ? property as OpenApiSchema
            : null;

    public static OpenApiSchema? ArrayItems(OpenApiSchema? schema) => schema?.Items as OpenApiSchema;

    public static void Patch(IDictionary<string, IOpenApiSchema> schemas,
                             string name,
                             Action<OpenApiSchema> describe)
    {
        if (schemas.TryGetValue(name, out IOpenApiSchema? node) && node is OpenApiSchema schema)
            describe(schema);
    }
}
