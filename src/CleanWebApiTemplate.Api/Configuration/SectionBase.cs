namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Base for the option sections the API reads. The section name is the class name without the
/// <c>Section</c> suffix, so <c>ConnectionStringsSection</c> maps to <c>ConnectionStrings</c>.
/// </summary>
public abstract class SectionBase
{
    public string SectionName { get; }

    protected SectionBase()
    {
        string typeName = GetType().Name;

        SectionName = typeName.EndsWith(AppSettings.SectionSuffix, StringComparison.Ordinal)
            ? typeName[..^AppSettings.SectionSuffix.Length]
            : throw new ArgumentException($"Section name {typeName} must end with {AppSettings.SectionSuffix}.");
    }
}
