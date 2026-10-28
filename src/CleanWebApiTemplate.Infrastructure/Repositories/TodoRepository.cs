using System.Text;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Constants;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace CleanWebApiTemplate.Infrastructure.Repositories;

/// <summary>
/// Pure ADO.NET (Npgsql) repository. Native AOT safe by construction:
/// no reflection, no expression trees, no dynamic mapping — every row is mapped
/// explicitly and every input value is sent as a bound parameter.
/// </summary>
public sealed class TodoRepository(INpgsqlConnectionFactory connectionFactory) : ITodoRepository
{
    private readonly INpgsqlConnectionFactory connectionFactory = connectionFactory;

    // Fixed column list/order shared by every full-row SELECT. The manual mapping in
    // MapTodo relies on this exact order. Built only from compile-time constants.
    private const string TodoColumns =
        "\"Id\", \"Title\", \"Description\", \"CreatedAt\", \"UpdatedAt\", \"Status\", \"CreatedBy\", \"UpdatedBy\"";

    private static readonly string SelectByIdSql =
        $"SELECT {TodoColumns} FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id;";

    private static readonly string SelectByIdAndCreatedBySql =
        $"SELECT {TodoColumns} FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id AND \"{TodoTable.CreatedByColumn}\" = @createdBy;";

    private static readonly string InsertSql = $"""
        INSERT INTO "{TodoTable.Name}" ({TodoColumns})
        VALUES (@id, @title, @description, @createdAt, @updatedAt, @status, @createdBy, @updatedBy);
        """;

    private static readonly string UpdateSql = $"""
        UPDATE "{TodoTable.Name}"
        SET "{TodoTable.TitleColumn}" = @title,
            "{TodoTable.DescriptionColumn}" = @description,
            "{TodoTable.CreatedAtColumn}" = @createdAt,
            "{TodoTable.UpdatedAtColumn}" = @updatedAt,
            "{TodoTable.StatusColumn}" = @status,
            "{TodoTable.CreatedByColumn}" = @createdBy,
            "{TodoTable.UpdatedByColumn}" = @updatedBy
        WHERE "{TodoTable.IdColumn}" = @id;
        """;

    private static readonly string DeleteSql =
        $"DELETE FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id;";

    private static readonly string DeleteByCreatedBySql =
        $"DELETE FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id AND \"{TodoTable.CreatedByColumn}\" = @createdBy;";

    public async Task<TodoEntity?> GetByIdAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(createdBy is null ? SelectByIdSql : SelectByIdAndCreatedBySql, connection);
        AddIdParameter(command, id);
        AddCreatedByParameter(command, createdBy);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTodo(reader) : null;
    }

    public async Task<IReadOnlyList<TodoDto>> GetTitlesAsync(int? pageNumber,
                                                             int? pageSize,
                                                             IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                             string? createdBy = null,
                                                             CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand();
        command.Connection = connection;

        var sql = new StringBuilder(
            $"SELECT \"{TodoTable.IdColumn}\", \"{TodoTable.TitleColumn}\" FROM \"{TodoTable.Name}\"");
        AppendCreatedByCondition(command, sql, createdBy);
        AppendOrderBy(sql, sortProperties);
        AppendPagination(command, sql, pageNumber, pageSize);
        command.CommandText = sql.ToString();

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        List<TodoDto> titles = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            titles.Add(new TodoDto
            {
                Id = reader.IsDBNull(0) ? null : new Ulid(reader.GetFieldValue<byte[]>(0)),
                Title = reader.IsDBNull(1) ? null : reader.GetString(1)
            });
        }

        return titles;
    }

    public async Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand();
        command.Connection = connection;

        var sql = new StringBuilder($"SELECT {TodoColumns} FROM \"{TodoTable.Name}\"");
        var where = new StringBuilder();
        int parameterIndex = 0;

        AppendInCondition(where, command, TodoTable.IdColumn, NpgsqlDbType.Bytea,
                          filter.Ids, static id => id.ToByteArray(), ref parameterIndex);
        AppendInCondition(where, command, TodoTable.TitleColumn, NpgsqlDbType.Text,
                          filter.Titles, static title => title, ref parameterIndex);
        AppendInCondition(where, command, TodoTable.StatusColumn, NpgsqlDbType.Integer,
                          filter.Statuses, static status => status, ref parameterIndex);
        AppendInCondition(where, command, TodoTable.CreatedByColumn, NpgsqlDbType.Text,
                          filter.CreatedBys, static createdBy => createdBy, ref parameterIndex);

        // The date range applies only when both bounds are provided (previous EF Core behavior).
        if (filter.StartDate is not null && filter.EndDate is not null)
        {
            AppendAnd(where);
            string startParameter = NextParameterName(ref parameterIndex);
            string endParameter = NextParameterName(ref parameterIndex);
            where.Append($"\"{TodoTable.CreatedAtColumn}\" >= {startParameter} AND \"{TodoTable.CreatedAtColumn}\" <= {endParameter}");
            // Stored as timestamptz; Npgsql requires an explicit UTC kind.
            command.Parameters.Add(startParameter, NpgsqlDbType.TimestampTz).Value = ToUtc(filter.StartDate.Value);
            command.Parameters.Add(endParameter, NpgsqlDbType.TimestampTz).Value = ToUtc(filter.EndDate.Value);
        }

        if (where.Length > 0)
            sql.Append(" WHERE ").Append(where);

        AppendOrderBy(sql, filter.SortProperties);
        AppendPagination(command, sql, filter.PageNumber, filter.PageSize);
        command.CommandText = sql.ToString();

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        List<TodoEntity> todos = [];
        while (await reader.ReadAsync(cancellationToken))
            todos.Add(MapTodo(reader));

        return todos;
    }

    public async Task<TodoEntity> InsertAsync(TodoEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(InsertSql, connection);
        AddEntityParameters(command, entity);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(UpdateSql, connection);
        AddEntityParameters(command, entity);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(Ulid id, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(createdBy is null ? DeleteSql : DeleteByCreatedBySql, connection);
        AddIdParameter(command, id);
        AddCreatedByParameter(command, createdBy);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> TitleExistsAsync(string title, Ulid? excludeId = null, CancellationToken cancellationToken = default)
    {
        string sql = excludeId is null
            ? $"SELECT COUNT(1) FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.TitleColumn}\" = @title;"
            : $"SELECT COUNT(1) FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.TitleColumn}\" = @title AND \"{TodoTable.IdColumn}\" <> @excludeId;";

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@title", NpgsqlDbType.Text).Value = title;
        if (excludeId is not null)
            command.Parameters.Add("@excludeId", NpgsqlDbType.Bytea).Value = excludeId.Value.ToByteArray();

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        return count > 0;
    }

    /// <summary>
    /// Explicit manual mapping. The reader must provide the columns in <see cref="TodoColumns"/> order.
    /// Every column is checked with IsDBNull before extraction.
    /// </summary>
    private static TodoEntity MapTodo(NpgsqlDataReader reader) => new()
    {
        // BYTEA (16 bytes) <-> Ulid.
        Id = reader.IsDBNull(0) ? Ulid.Empty : new Ulid(reader.GetFieldValue<byte[]>(0)),
        Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        // TIMESTAMPTZ <-> DateTime (Npgsql returns Kind=Utc).
        CreatedAt = reader.IsDBNull(3) ? default : reader.GetDateTime(3),
        UpdatedAt = reader.IsDBNull(4) ? default : reader.GetDateTime(4),
        Status = reader.IsDBNull(5) ? default : reader.GetInt32(5),
        CreatedBy = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
        UpdatedBy = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
    };

    private static void AddIdParameter(NpgsqlCommand command, Ulid id) =>
        command.Parameters.Add("@id", NpgsqlDbType.Bytea).Value = id.ToByteArray();

    private static void AddCreatedByParameter(NpgsqlCommand command, string? createdBy)
    {
        if (createdBy is not null)
            command.Parameters.Add("@createdBy", NpgsqlDbType.Text).Value = createdBy;
    }

    private static void AppendCreatedByCondition(NpgsqlCommand command, StringBuilder sql, string? createdBy)
    {
        if (createdBy is null)
            return;

        sql.Append($" WHERE \"{TodoTable.CreatedByColumn}\" = @createdBy");
        command.Parameters.Add("@createdBy", NpgsqlDbType.Text).Value = createdBy;
    }

    private static void AddEntityParameters(NpgsqlCommand command, TodoEntity entity)
    {
        AddIdParameter(command, entity.Id);
        command.Parameters.Add("@title", NpgsqlDbType.Text).Value = entity.Title;
        command.Parameters.Add("@description", NpgsqlDbType.Text).Value = entity.Description;
        command.Parameters.Add("@createdAt", NpgsqlDbType.TimestampTz).Value = ToUtc(entity.CreatedAt);
        command.Parameters.Add("@updatedAt", NpgsqlDbType.TimestampTz).Value = ToUtc(entity.UpdatedAt);
        command.Parameters.Add("@status", NpgsqlDbType.Integer).Value = entity.Status;
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
    /// Appends the ORDER BY clause resolving property names through the
    /// <see cref="TodoSortColumns"/> whitelist. Direction comes from a boolean,
    /// never from raw input text.
    /// </summary>
    private static void AppendOrderBy(StringBuilder sql, IEnumerable<KeyValuePair<string, bool>>? sortProperties)
    {
        if (sortProperties is null)
            return;

        bool isFirstSort = true;
        foreach (var (propertyName, descending) in sortProperties)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(sortProperties));

            if (TodoSortColumns.Map.TryGetValue(propertyName.Trim(), out string? columnName) is false)
                throw new ArgumentException($"Property '{propertyName}' is not a valid sort property.", nameof(sortProperties));

            sql.Append(isFirstSort ? " ORDER BY " : ", ");
            sql.Append('"').Append(columnName).Append(descending ? "\" DESC" : "\" ASC");
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

    /// <summary>
    /// PostgreSQL timestamptz values must be UTC. API input deserialized without an
    /// offset arrives as <see cref="DateTimeKind.Unspecified"/>; treat it as UTC.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
