using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using static CleanWebApiTemplate.Api.Configuration.OpenApiSchemaHelpers;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>Schema descriptions shared by every operation.</summary>
internal static class CommonOpenApiSchemas
{
    public static void DescribeSchemas(OpenApiDocument document)
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
    }
}
