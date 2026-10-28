using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

internal static class OpenApiExamplesError
{
    public static string ToJson(IReadOnlyList<string> problems)
    {
        var array = new JsonArray();
        foreach (var problem in problems)
        {
            JsonNode? value = JsonValue.Create(problem);
            array.Add(value);
        }

        return new JsonObject
        {
            ["error"] = "The local OpenAPI examples could not be applied.",
            ["problems"] = array,
        }.ToJsonString();
    }
}
