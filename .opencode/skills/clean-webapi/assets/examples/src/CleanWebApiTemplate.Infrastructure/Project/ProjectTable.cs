namespace CleanWebApiTemplate.Infrastructure.Project;

/// <summary>
/// Schema metadata for the Project table: the single source of truth for the table and column names used
/// to build fixed, injection-safe SQL text. Business constraints live in <c>Domain.Project.ProjectConstraints</c>.
/// </summary>
public static class ProjectTable
{
    public const string Name = "Project";

    public const string IdColumn = "Id";
    public const string NameColumn = "Name";
    public const string DescriptionColumn = "Description";
    public const string CreatedAtColumn = "CreatedAt";
    public const string UpdatedAtColumn = "UpdatedAt";
    public const string StatusColumn = "Status";
    public const string CreatedByColumn = "CreatedBy";
    public const string UpdatedByColumn = "UpdatedBy";

    /// <summary>Unique index that makes the name unique per owner at the database level.</summary>
    public const string UniqueNameIndexName = "UX_Project_CreatedBy_Name";

    /// <summary>Primary key constraint, so a duplicate id is never reported as a duplicate name.</summary>
    public const string PrimaryKeyName = "PK_Project";

    /// <summary>Every column of a full row, in the order the repository maps them.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        IdColumn,
        NameColumn,
        DescriptionColumn,
        CreatedAtColumn,
        UpdatedAtColumn,
        StatusColumn,
        CreatedByColumn,
        UpdatedByColumn,
    ];
}
