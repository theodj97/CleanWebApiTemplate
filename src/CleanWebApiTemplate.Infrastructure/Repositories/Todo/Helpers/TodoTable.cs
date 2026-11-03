namespace CleanWebApiTemplate.Infrastructure.Repositories.Todo.Helpers;

/// <summary>
/// Schema metadata for the "Todo" table. Single source of truth for table and column
/// names used to build fixed, injection-safe SQL text. Business constraints live in
/// <c>Domain.Models.Todo.TodoConstraints</c>.
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

    // The table DDL lives in local/database/*.sql so there is a single schema source.
}
