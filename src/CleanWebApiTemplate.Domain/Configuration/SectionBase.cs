namespace CleanWebApiTemplate.Domain.Configuration;

public abstract class SectionBase
{
    public string SectionName { get; }

    protected SectionBase()
    {
        string typeName = GetType().Name;

        SectionName = typeName.EndsWith(AppSettings.SECTION_EXTENSION)
            ? typeName[..^AppSettings.SECTION_EXTENSION.Length]
            : throw new ArgumentException($"Section name {typeName} doesn't match syntax finishing in {AppSettings.SECTION_EXTENSION}.");
    }
}
