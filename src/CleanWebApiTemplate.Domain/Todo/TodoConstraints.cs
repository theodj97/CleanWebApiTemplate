namespace CleanWebApiTemplate.Domain.Todo;

/// <summary>Business constraints of a Todo. They live in the domain, not in the SQL schema.</summary>
public static class TodoConstraints
{
    public const int TitleMaxLength = 255;
    public const int DescriptionMaxLength = 1000;
    public const int OwnerMaxLength = 255;
}
