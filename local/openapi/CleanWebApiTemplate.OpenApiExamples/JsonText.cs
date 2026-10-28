using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

internal static class JsonText
{
    public static string? Read(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static string[] ReadStrings(JsonNode? node) =>
        node is JsonArray array ? [.. array.Select(item => Read(item) ?? string.Empty)] : [];
}
