namespace CleanWebApiTemplate.Domain.Configuration;

public abstract class SectionBase
{
    public string SectionName { get; }

    protected SectionBase()
    {
        string typeName = GetType().Name;

        if (typeName.EndsWith(AppSettings.SECTION_EXTENSION))
            SectionName = typeName[..^AppSettings.SECTION_EXTENSION.Length];
        else
            throw new ArgumentException($"Section name {typeName} doesn't match syntax finishing in {AppSettings.SECTION_EXTENSION}.");
    }
}
