namespace {App}.Infrastructure.Repositories.Project.Helpers;

/// <summary>
/// Schema metadata for the "Project" table. Single source of truth for table and column names used to
/// build fixed, injection-safe SQL text. Business constraints live in
/// <c>Domain.Models.Project.ProjectConstraints</c>.
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

    // The table DDL lives in local/database/*.sql so there is a single schema source.
}
