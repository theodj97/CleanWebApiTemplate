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

    /// <summary>
    /// DDL for the Todo table. Idempotent, so it can run at every startup/test setup.
    /// DateTime values are stored as TEXT in the "yyyy-MM-dd HH:mm:ss.fffffff" format
    /// (the default Microsoft.Data.Sqlite DateTime binding format).
    /// </summary>
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS "Todo" (
            "Id"          BLOB    NOT NULL,
            "Title"       TEXT    NOT NULL,
            "Description" TEXT    NULL,
            "CreatedAt"   TEXT    NOT NULL,
            "UpdatedAt"   TEXT    NOT NULL,
            "Status"      INTEGER NOT NULL,
            "CreatedBy"   TEXT    NOT NULL,
            "UpdatedBy"   TEXT    NOT NULL,
            CONSTRAINT "PK_Todo" PRIMARY KEY ("Id")
        );
        """;
}
