namespace CleanWebApiTemplate.Domain.Models.Todo;

/// <summary>Business constraints for a TODO. Kept in the domain, not in the SQL schema.</summary>
public static class TodoConstraints
{
    public const int TitleMaxLength = 255;
    public const int DescriptionMaxLength = 1000;
    public const int CreatedByMaxLength = 255;
    public const int UpdatedByMaxLength = 255;
}
