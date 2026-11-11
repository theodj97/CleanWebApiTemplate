namespace CleanWebApiTemplate.Infrastructure.Todo;

/// <summary>
/// Schema metadata for the Todo table: the single source of truth for the table and column names used
/// to build fixed, injection-safe SQL text. Business constraints live in <c>Domain.Todo.TodoConstraints</c>.
/// </summary>
public static class TodoTable
{
    public const string Name = "Todo";

    public const string IdColumn = "Id";
    public const string TitleColumn = "Title";
    public const string DescriptionColumn = "Description";
    public const string CreatedAtColumn = "CreatedAt";
    public const string UpdatedAtColumn = "UpdatedAt";
    public const string StatusColumn = "Status";
    public const string CreatedByColumn = "CreatedBy";
    public const string UpdatedByColumn = "UpdatedBy";

    /// <summary>Unique index that makes the title unique per owner at the database level.</summary>
    public const string UniqueTitleIndexName = "UX_Todo_CreatedBy_Title";

    /// <summary>Primary key constraint, so a duplicate id is never reported as a duplicate title.</summary>
    public const string PrimaryKeyName = "PK_Todo";

    /// <summary>Every column of a full row, in the order the repository maps them.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        IdColumn,
        TitleColumn,
        DescriptionColumn,
        CreatedAtColumn,
        UpdatedAtColumn,
        StatusColumn,
        CreatedByColumn,
        UpdatedByColumn,
    ];
}
