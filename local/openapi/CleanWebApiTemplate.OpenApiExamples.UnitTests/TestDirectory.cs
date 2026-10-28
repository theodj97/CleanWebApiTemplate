namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

internal sealed class TestDirectory : IDisposable
{
    public TestDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), "openapi-examples-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public void Write(string relativePath, string content)
    {
        var fullPath = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
