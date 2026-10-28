using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Fetches the API OpenAPI document and applies the catalog on every request. Nothing is cached:
/// the seed and the document are rebuilt on every AppHost start.
/// </summary>
internal sealed class OpenApiDocumentService(
    HttpClient client,
    ExamplesCatalog catalog,
    ILogger<OpenApiDocumentService> logger)
{
    public async Task<JsonNode> GetDocumentAsync(CancellationToken cancellationToken)
    {
        var documentJson = await client.GetStringAsync("/openapi/v1.json", cancellationToken);

        JsonObject document;
        try
        {
            document = JsonNode.Parse(documentJson) as JsonObject
                ?? throw new OpenApiExamplesException(["The API OpenAPI document is not a JSON object."]);
        }
        catch (JsonException exception)
        {
            throw new OpenApiExamplesException([$"The API OpenAPI document is not valid JSON: {exception.Message}"]);
        }

        var catalogJson = await ReadCatalogAsync(cancellationToken);
        var catalogNode = ParseCatalog(catalogJson);

        var problems = OpenApiExamplesMerger.Merge(document, catalogNode, catalog.BasePath);
        if (problems.Count > 0)
        {
            foreach (var problem in problems)
                logger.LogError("OpenAPI examples: {Problem}", problem);

            throw new OpenApiExamplesException(problems);
        }

        return document;
    }

    private async Task<string> ReadCatalogAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(catalog.CatalogPath, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new OpenApiExamplesException([$"Could not read '{catalog.CatalogPath}': {exception.Message}"]);
        }
    }

    private JsonObject ParseCatalog(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject
                ?? throw new OpenApiExamplesException([$"'{catalog.CatalogPath}' is not a JSON object."]);
        }
        catch (JsonException exception)
        {
            throw new OpenApiExamplesException([$"'{catalog.CatalogPath}' is not valid JSON: {exception.Message}"]);
        }
    }
}
