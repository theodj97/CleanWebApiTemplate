namespace CleanWebApiTemplate.Domain.Project;

/// <summary>Business constraints of a Project. They live in the domain, not in the SQL schema.</summary>
public static class ProjectConstraints
{
    public const int NameMaxLength = 255;
    public const int DescriptionMaxLength = 1000;
    public const int OwnerMaxLength = 255;
}
