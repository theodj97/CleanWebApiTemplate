using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Constants;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Text;

namespace CleanWebApiTemplate.Infrastructure.Repositories;

/// <summary>
/// Pure ADO.NET (Microsoft.Data.Sqlite) repository. Native AOT safe by construction:
/// no reflection, no expression trees, no dynamic mapping — every row is mapped
/// explicitly and every input value is sent as a bound parameter.
/// </summary>
public sealed class TodoRepository(ISqliteConnectionFactory connectionFactory) : ITodoRepository
{
    private readonly ISqliteConnectionFactory connectionFactory = connectionFactory;

    // Fixed column list/order shared by every full-row SELECT. The manual mapping in
    // MapTodo relies on this exact order. Built only from compile-time constants.
    private const string TodoColumns =
        "\"Id\", \"Title\", \"Description\", \"CreatedAt\", \"UpdatedAt\", \"Status\", \"CreatedBy\", \"UpdatedBy\"";

    private static readonly string SelectByIdSql =
        $"SELECT {TodoColumns} FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id;";

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

    public async Task<TodoEntity?> GetByIdAsync(Ulid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new SqliteCommand(SelectByIdSql, connection);
        AddIdParameter(command, id);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTodo(reader) : null;
    }

    public async Task<IReadOnlyList<TodoDto>> GetTitlesAsync(int? pageNumber,
                                                             int? pageSize,
                                                             IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                             CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new SqliteCommand();
        command.Connection = connection;

        var sql = new StringBuilder(
            $"SELECT \"{TodoTable.IdColumn}\", \"{TodoTable.TitleColumn}\" FROM \"{TodoTable.Name}\"");
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

        using var command = new SqliteCommand();
        command.Connection = connection;

        var sql = new StringBuilder($"SELECT {TodoColumns} FROM \"{TodoTable.Name}\"");
        var where = new StringBuilder();
        int parameterIndex = 0;

        AppendInCondition(where, command, TodoTable.IdColumn, SqliteType.Blob,
                          filter.Ids, static id => id.ToByteArray(), ref parameterIndex);
        AppendInCondition(where, command, TodoTable.TitleColumn, SqliteType.Text,
                          filter.Titles, static title => title, ref parameterIndex);
        AppendInCondition(where, command, TodoTable.StatusColumn, SqliteType.Integer,
                          filter.Statuses, static status => status, ref parameterIndex);
        AppendInCondition(where, command, TodoTable.CreatedByColumn, SqliteType.Text,
                          filter.CreatedBys, static createdBy => createdBy, ref parameterIndex);

        // The date range applies only when both bounds are provided (previous EF Core behavior).
        if (filter.StartDate is not null && filter.EndDate is not null)
        {
            AppendAnd(where);
            string startParameter = NextParameterName(ref parameterIndex);
            string endParameter = NextParameterName(ref parameterIndex);
            where.Append($"\"{TodoTable.CreatedAtColumn}\" >= {startParameter} AND \"{TodoTable.CreatedAtColumn}\" <= {endParameter}");
            // Bound as DateTime: stored as TEXT "yyyy-MM-dd HH:mm:ss.fffffff" (fixed-width,
            // so lexicographic comparison is chronological), same format EF Core used.
            command.Parameters.AddWithValue(startParameter, filter.StartDate.Value);
            command.Parameters.AddWithValue(endParameter, filter.EndDate.Value);
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

        using var command = new SqliteCommand(InsertSql, connection);
        AddEntityParameters(command, entity);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> UpdateAsync(TodoEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new SqliteCommand(UpdateSql, connection);
        AddEntityParameters(command, entity);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(Ulid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new SqliteCommand(DeleteSql, connection);
        AddIdParameter(command, id);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> TitleExistsAsync(string title, Ulid? excludeId = null, CancellationToken cancellationToken = default)
    {
        string sql = excludeId is null
            ? $"SELECT COUNT(1) FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.TitleColumn}\" = @title;"
            : $"SELECT COUNT(1) FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.TitleColumn}\" = @title AND \"{TodoTable.IdColumn}\" <> @excludeId;";

        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.Add("@title", SqliteType.Text).Value = title;
        if (excludeId is not null)
            command.Parameters.Add("@excludeId", SqliteType.Blob).Value = excludeId.Value.ToByteArray();

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        return count > 0;
    }

    /// <summary>
    /// Explicit manual mapping. The reader must provide the columns in <see cref="TodoColumns"/> order.
    /// Every column is checked with IsDBNull before extraction.
    /// </summary>
    private static TodoEntity MapTodo(SqliteDataReader reader) => new()
    {
        // BLOB (16 bytes) <-> Ulid.
        Id = reader.IsDBNull(0) ? Ulid.Empty : new Ulid(reader.GetFieldValue<byte[]>(0)),
        Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        // TEXT "yyyy-MM-dd HH:mm:ss.fffffff" <-> DateTime.
        CreatedAt = reader.IsDBNull(3) ? default : reader.GetDateTime(3),
        UpdatedAt = reader.IsDBNull(4) ? default : reader.GetDateTime(4),
        Status = reader.IsDBNull(5) ? default : reader.GetInt32(5),
        CreatedBy = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
        UpdatedBy = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
    };

    private static void AddIdParameter(SqliteCommand command, Ulid id) =>
        command.Parameters.Add("@id", SqliteType.Blob).Value = id.ToByteArray();

    private static void AddEntityParameters(SqliteCommand command, TodoEntity entity)
    {
        AddIdParameter(command, entity.Id);
        command.Parameters.Add("@title", SqliteType.Text).Value = entity.Title;
        command.Parameters.Add("@description", SqliteType.Text).Value = entity.Description;
        command.Parameters.Add("@createdAt", SqliteType.Text).Value = entity.CreatedAt;
        command.Parameters.Add("@updatedAt", SqliteType.Text).Value = entity.UpdatedAt;
        command.Parameters.Add("@status", SqliteType.Integer).Value = entity.Status;
        command.Parameters.Add("@createdBy", SqliteType.Text).Value = entity.CreatedBy;
        command.Parameters.Add("@updatedBy", SqliteType.Text).Value = entity.UpdatedBy;
    }

    /// <summary>
    /// Appends a "column IN (@p0, @p1, ...)" condition. Every value becomes its own bound
    /// parameter; only the fixed internal column name is embedded in the SQL text.
    /// </summary>
    private static void AppendInCondition<T>(StringBuilder where,
                                             SqliteCommand command,
                                             string columnName,
                                             SqliteType columnType,
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

    private static void AppendPagination(SqliteCommand command, StringBuilder sql, int? pageNumber, int? pageSize)
    {
        if (pageNumber is null || pageSize is null)
            return;

        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");

        sql.Append(" LIMIT @pageSize OFFSET @offset");
        command.Parameters.Add("@pageSize", SqliteType.Integer).Value = pageSize.Value;
        command.Parameters.Add("@offset", SqliteType.Integer).Value = (pageNumber.Value - 1) * pageSize.Value;
    }

    private static void AppendAnd(StringBuilder where)
    {
        if (where.Length > 0)
            where.Append(" AND ");
    }

    private static string NextParameterName(ref int parameterIndex) => $"@p{parameterIndex++}";
}
