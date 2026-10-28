namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Paths of the local examples layer: the catalog and the body files it references.
/// </summary>
internal sealed class ExamplesCatalog
{
    public ExamplesCatalog(string basePath)
    {
        BasePath = Path.GetFullPath(basePath);
        CatalogPath = Path.Combine(BasePath, "examples.json");
    }

    public string BasePath { get; }

    public string CatalogPath { get; }
}
