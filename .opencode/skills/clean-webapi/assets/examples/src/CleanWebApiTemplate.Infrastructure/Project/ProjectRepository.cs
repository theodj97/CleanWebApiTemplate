using System.Globalization;
using System.Text;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Domain.Project;
using Npgsql;
using NpgsqlTypes;

namespace CleanWebApiTemplate.Infrastructure.Project;

/// <summary>
/// Pure ADO.NET (Npgsql) repository. Native AOT safe by construction: no reflection, no expression
/// trees, no dynamic mapping. Every row is mapped explicitly, every value is a bound parameter and
/// every identifier comes from <see cref="ProjectTable"/>.
/// </summary>
public sealed class ProjectRepository(NpgsqlDataSource dataSource) : IProjectRepository
{
    private readonly NpgsqlDataSource dataSource = dataSource;

    // Fixed column list/order shared by every full-row SELECT. The manual mapping in MapProject relies on
    // this exact order, and the names come from ProjectTable, so the schema has one source of truth.
    private static readonly string ProjectColumns =
        string.Join(", ", ProjectTable.Columns.Select(column => $"\"{column}\""));

    private const string DuplicateNameMessage = "A Project with this name already exists for this owner.";

    private static readonly string SelectByIdSql =
        $"SELECT {ProjectColumns} FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.IdColumn}\" = @id;";

    private static readonly string SelectByIdAndOwnerSql =
        $"SELECT {ProjectColumns} FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.IdColumn}\" = @id AND \"{ProjectTable.CreatedByColumn}\" = @visibleOwner;";

    private static readonly string InsertSql = $"""
        INSERT INTO "{ProjectTable.Name}" ({ProjectColumns})
        VALUES (@id, @name, @description, @createdAt, @updatedAt, @status, @createdBy, @updatedBy);
        """;

    private static readonly string UpdateSql = $"""
        UPDATE "{ProjectTable.Name}"
        SET "{ProjectTable.NameColumn}" = @name,
            "{ProjectTable.DescriptionColumn}" = @description,
            "{ProjectTable.CreatedAtColumn}" = @createdAt,
            "{ProjectTable.UpdatedAtColumn}" = @updatedAt,
            "{ProjectTable.StatusColumn}" = @status,
            "{ProjectTable.CreatedByColumn}" = @createdBy,
            "{ProjectTable.UpdatedByColumn}" = @updatedBy
        WHERE "{ProjectTable.IdColumn}" = @id;
        """;

    private static readonly string DeleteSql =
        $"DELETE FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.IdColumn}\" = @id;";

    private static readonly string DeleteByOwnerSql =
        $"DELETE FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.IdColumn}\" = @id AND \"{ProjectTable.CreatedByColumn}\" = @visibleOwner;";

    public async Task<ProjectEntity?> GetByIdAsync(Ulid id,
                                               string? visibleOwner = null,
                                               CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(visibleOwner is null ? SelectByIdSql : SelectByIdAndOwnerSql, connection);
        AddIdParameter(command, id);
        AddVisibleOwnerParameter(command, visibleOwner);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapProject(reader) : null;
    }

    public async Task<IReadOnlyList<ProjectName>> GetNamesAsync(int? pageNumber,
                                                               int? pageSize,
                                                               IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                               string? visibleOwner = null,
                                                               CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        using var command = new NpgsqlCommand { Connection = connection };

        var sql = new StringBuilder(
            $"SELECT \"{ProjectTable.IdColumn}\", \"{ProjectTable.NameColumn}\" FROM \"{ProjectTable.Name}\"");
        AppendVisibleOwnerCondition(command, sql, visibleOwner);
        AppendOrderBy(sql, sortProperties);
        AppendPagination(command, sql, pageNumber, pageSize);
        command.CommandText = sql.ToString();

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        List<ProjectName> names = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            Ulid id = reader.IsDBNull(0) ? Ulid.Empty : new Ulid(reader.GetFieldValue<byte[]>(0));
            string name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            names.Add(new ProjectName(id, name));
        }

        return names;
    }

    public async Task<IReadOnlyList<ProjectEntity>> SearchAsync(ProjectFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        using var command = new NpgsqlCommand { Connection = connection };

        var sql = new StringBuilder($"SELECT {ProjectColumns} FROM \"{ProjectTable.Name}\"");
        var where = new StringBuilder();
        int parameterIndex = 0;

        AppendInCondition(where, command, ProjectTable.IdColumn, NpgsqlDbType.Bytea,
                          filter.Ids, static id => id.ToByteArray(), ref parameterIndex);
        AppendNameContainsCondition(where, command, filter.Names, ref parameterIndex);
        AppendInCondition(where, command, ProjectTable.StatusColumn, NpgsqlDbType.Integer,
                          filter.Statuses, static status => status, ref parameterIndex);
        AppendInCondition(where, command, ProjectTable.CreatedByColumn, NpgsqlDbType.Text,
                          filter.Owners, static owner => owner, ref parameterIndex);

        // The date range applies only when both bounds are provided (validated upstream).
        if (filter.StartDate is not null && filter.EndDate is not null)
        {
            AppendAnd(where);
            string startParameter = NextParameterName(ref parameterIndex);
            string endParameter = NextParameterName(ref parameterIndex);
            where.Append(CultureInfo.InvariantCulture,
                         $"\"{ProjectTable.CreatedAtColumn}\" >= {startParameter} AND \"{ProjectTable.CreatedAtColumn}\" <= {endParameter}");
            // Stored as timestamptz; Npgsql requires an explicit UTC kind.
            command.Parameters.Add(startParameter, NpgsqlDbType.TimestampTz).Value = ToUtc(filter.StartDate.Value);
            command.Parameters.Add(endParameter, NpgsqlDbType.TimestampTz).Value = ToUtc(filter.EndDate.Value);
        }

        if (where.Length > 0)
            sql.Append(" WHERE ").Append(where);

        AppendOrderBy(sql, filter.SortProperties);
        AppendPagination(command, sql, filter.PageNumber, filter.PageSize);
        command.CommandText = sql.ToString();

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        List<ProjectEntity> projects = [];
        while (await reader.ReadAsync(cancellationToken))
            projects.Add(MapProject(reader));

        return projects;
    }

    public async Task<Result<ProjectEntity>> InsertAsync(ProjectEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(InsertSql, connection);
        AddEntityParameters(command, entity);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (IsDuplicateName(exception))
        {
            // Business outcomes are returned as results, not thrown as controlled exceptions.
            return Result<ProjectEntity>.Failure(new ConflictError(DuplicateNameMessage));
        }

        return Result<ProjectEntity>.Success(entity);
    }

    public async Task<Result<bool>> UpdateAsync(ProjectEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(UpdateSql, connection);
        AddEntityParameters(command, entity);

        try
        {
            return Result<bool>.Success(await command.ExecuteNonQueryAsync(cancellationToken) > 0);
        }
        catch (PostgresException exception) when (IsDuplicateName(exception))
        {
            return Result<bool>.Failure(new ConflictError(DuplicateNameMessage));
        }
    }

    public async Task<bool> DeleteAsync(Ulid id,
                                       string? visibleOwner = null,
                                       CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(visibleOwner is null ? DeleteSql : DeleteByOwnerSql, connection);
        AddIdParameter(command, id);
        AddVisibleOwnerParameter(command, visibleOwner);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> NameExistsAsync(string name,
                                            string owner,
                                            Ulid? excludeId = null,
                                            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        string sql = excludeId is null
            ? $"SELECT COUNT(1) FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.NameColumn}\" = @name AND \"{ProjectTable.CreatedByColumn}\" = @owner;"
            : $"SELECT COUNT(1) FROM \"{ProjectTable.Name}\" WHERE \"{ProjectTable.NameColumn}\" = @name AND \"{ProjectTable.CreatedByColumn}\" = @owner AND \"{ProjectTable.IdColumn}\" <> @excludeId;";

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@name", NpgsqlDbType.Text).Value = name;
        command.Parameters.Add("@owner", NpgsqlDbType.Text).Value = owner;
        if (excludeId is not null)
            command.Parameters.Add("@excludeId", NpgsqlDbType.Bytea).Value = excludeId.Value.ToByteArray();

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        return count > 0;
    }

    /// <summary>
    /// Explicit manual mapping. The reader must provide the columns in <see cref="ProjectTable.Columns"/> order.
    /// Every column is checked with IsDBNull before extraction.
    /// </summary>
    private static ProjectEntity MapProject(NpgsqlDataReader reader) => new()
    {
        // BYTEA (16 bytes) <-> Ulid.
        Id = reader.IsDBNull(0) ? Ulid.Empty : new Ulid(reader.GetFieldValue<byte[]>(0)),
        Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        // TIMESTAMPTZ <-> DateTime (Npgsql returns Kind=Utc).
        CreatedAt = reader.IsDBNull(3) ? default : reader.GetDateTime(3),
        UpdatedAt = reader.IsDBNull(4) ? default : reader.GetDateTime(4),
        // INTEGER <-> enum.
        Status = reader.IsDBNull(5) ? EProjectStatus.Draft : (EProjectStatus)reader.GetInt32(5),
        CreatedBy = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
        UpdatedBy = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
    };

    private static void AddIdParameter(NpgsqlCommand command, Ulid id) =>
        command.Parameters.Add("@id", NpgsqlDbType.Bytea).Value = id.ToByteArray();

    private static void AddVisibleOwnerParameter(NpgsqlCommand command, string? visibleOwner)
    {
        if (visibleOwner is not null)
            command.Parameters.Add("@visibleOwner", NpgsqlDbType.Text).Value = visibleOwner;
    }

    private static void AppendVisibleOwnerCondition(NpgsqlCommand command, StringBuilder sql, string? visibleOwner)
    {
        if (visibleOwner is null)
            return;

        sql.Append($" WHERE \"{ProjectTable.CreatedByColumn}\" = @visibleOwner");
        command.Parameters.Add("@visibleOwner", NpgsqlDbType.Text).Value = visibleOwner;
    }

    private static void AddEntityParameters(NpgsqlCommand command, ProjectEntity entity)
    {
        AddIdParameter(command, entity.Id);
        command.Parameters.Add("@name", NpgsqlDbType.Text).Value = entity.Name;
        command.Parameters.Add("@description", NpgsqlDbType.Text).Value = entity.Description;
        command.Parameters.Add("@createdAt", NpgsqlDbType.TimestampTz).Value = ToUtc(entity.CreatedAt);
        command.Parameters.Add("@updatedAt", NpgsqlDbType.TimestampTz).Value = ToUtc(entity.UpdatedAt);
        command.Parameters.Add("@status", NpgsqlDbType.Integer).Value = (int)entity.Status;
        command.Parameters.Add("@createdBy", NpgsqlDbType.Text).Value = entity.CreatedBy;
        command.Parameters.Add("@updatedBy", NpgsqlDbType.Text).Value = entity.UpdatedBy;
    }

    /// <summary>
    /// Appends a "column IN (@p0, @p1, ...)" condition. Every value becomes its own bound
    /// parameter; only the fixed internal column name is embedded in the SQL text.
    /// </summary>
    private static void AppendInCondition<T>(StringBuilder where,
                                             NpgsqlCommand command,
                                             string columnName,
                                             NpgsqlDbType columnType,
                                             IReadOnlyList<T>? values,
                                             Func<T, object> toDbValue,
                                             ref int parameterIndex)
    {
        if (values is null || values.Count is 0)
            return;

        AppendAnd(where);
        where.Append('"').Append(columnName).Append("\" IN (");
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
                where.Append(", ");

            string parameterName = NextParameterName(ref parameterIndex);
            where.Append(parameterName);
            command.Parameters.Add(parameterName, columnType).Value = toDbValue(values[i]);
        }
        where.Append(')');
    }

    /// <summary>
    /// Appends a case-insensitive "name contains any of these fragments" condition. Each
    /// fragment is a bound parameter; LIKE wildcards in user input are escaped.
    /// </summary>
    private static void AppendNameContainsCondition(StringBuilder where,
                                                     NpgsqlCommand command,
                                                     IReadOnlyList<string>? names,
                                                     ref int parameterIndex)
    {
        if (names is null || names.Count is 0)
            return;

        string[] fragments = [.. names.Where(name => string.IsNullOrWhiteSpace(name) is false)];
        if (fragments.Length is 0)
            return;

        AppendAnd(where);
        where.Append('(');
        for (int i = 0; i < fragments.Length; i++)
        {
            if (i > 0)
                where.Append(" OR ");

            string parameterName = NextParameterName(ref parameterIndex);
            where.Append(CultureInfo.InvariantCulture,
                         $"\"{ProjectTable.NameColumn}\" ILIKE {parameterName} ESCAPE '\\'");
            command.Parameters.Add(parameterName, NpgsqlDbType.Text).Value = $"%{EscapeLike(fragments[i])}%";
        }
        where.Append(')');
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("%", "\\%", StringComparison.Ordinal)
             .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>
    /// Appends the ORDER BY clause resolving property names through the <see cref="ProjectSortColumns"/>
    /// whitelist. Direction comes from a boolean, never from raw input text.
    /// </summary>
    private static void AppendOrderBy(StringBuilder sql, IEnumerable<KeyValuePair<string, bool>>? sortProperties)
    {
        if (sortProperties is null)
            return;

        bool isFirstSort = true;
        foreach (KeyValuePair<string, bool> sort in sortProperties)
        {
            if (string.IsNullOrWhiteSpace(sort.Key))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(sortProperties));

            if (ProjectSortColumns.Map.TryGetValue(sort.Key.Trim(), out string? columnName) is false)
                throw new ArgumentException($"Property '{sort.Key}' is not a valid sort property.", nameof(sortProperties));

            sql.Append(isFirstSort ? " ORDER BY " : ", ");
            sql.Append('"').Append(columnName).Append(sort.Value ? "\" DESC" : "\" ASC");
            isFirstSort = false;
        }
    }

    private static void AppendPagination(NpgsqlCommand command, StringBuilder sql, int? pageNumber, int? pageSize)
    {
        if (pageNumber is null || pageSize is null)
            return;

        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");

        sql.Append(" LIMIT @pageSize OFFSET @offset");
        command.Parameters.Add("@pageSize", NpgsqlDbType.Integer).Value = pageSize.Value;
        command.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = (pageNumber.Value - 1) * pageSize.Value;
    }

    private static void AppendAnd(StringBuilder where)
    {
        if (where.Length > 0)
            where.Append(" AND ");
    }

    private static string NextParameterName(ref int parameterIndex) => $"@p{parameterIndex++}";

    private static bool IsDuplicateName(PostgresException exception) =>
        exception.SqlState == PostgresErrorCodes.UniqueViolation
        && exception.ConstraintName == ProjectTable.UniqueNameIndexName;

    /// <summary>
    /// PostgreSQL timestamptz values must be UTC. API input deserialized without an
    /// offset arrives as <see cref="DateTimeKind.Unspecified"/>; treat it as UTC.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
