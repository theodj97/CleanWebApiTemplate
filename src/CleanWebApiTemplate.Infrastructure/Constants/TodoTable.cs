namespace CleanWebApiTemplate.Infrastructure.Constants;

/// <summary>
/// Schema metadata for the "Todo" table. Single source of truth for table/column
/// names (used to build fixed, injection-safe SQL text) and column constraints
/// (used by the application-layer validators).
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

    public static byte TitleMaxLength => 255;
    public static int DescriptionMaxLength => 1000;
    public static byte CreatedByMaxLength => 255;
    public static byte UpdatedByMaxLength => 255;

    // The table DDL lives in local/database/*.sql so there is a single schema source.
}
