namespace {App}.Domain.Models.Project;

/// <summary>Business constraints for a project. Referenced by validators, the seed and the docs.</summary>
public static class ProjectConstraints
{
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 1000;
    public const int OwnerMaxLength = 255;
}
