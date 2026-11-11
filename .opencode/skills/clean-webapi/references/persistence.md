# Persistence

PostgreSQL through Npgsql, hand-written ADO.NET, one data source per process. This reference covers the
repository, the SQL scripts and the test data access.

## NpgsqlDataSource

`ConfigureServices` registers one singleton for the process:

```csharp
services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
```

The repository receives it and opens a connection per operation:

```csharp
public sealed class TodoRepository(NpgsqlDataSource dataSource) : ITodoRepository
{
    private readonly NpgsqlDataSource dataSource = dataSource;

    public async Task<TodoEntity?> GetByIdAsync(Ulid id, string? visibleOwner = null, CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        // bind, execute, map
    }
}
```

The data source owns the pool. Do not build a connection string per call, do not cache a connection, do not
share one across requests. The connection string comes from OpenBao (see `configuration-and-secrets.md`).

## The table helper

`{Feature}Table` is the single source of the identifiers:

```csharp
public static class TodoTable
{
    public const string Name = "Todo";
    public const string IdColumn = "Id";
    public const string TitleColumn = "Title";
    // … one per column
    public const string UniqueTitleIndexName = "UX_Todo_CreatedBy_Title";
    public const string PrimaryKeyName = "PK_Todo";

    public static readonly IReadOnlyList<string> Columns = [IdColumn, TitleColumn, …];
}
```

- The repository builds its column list from `Columns`, so the schema and the mapper cannot drift on names.
- The `Map{Feature}` method relies on the order of `Columns`. Changing the order means changing the mapper.
- The unique index name is part of the contract: the repository only maps **that** violation to a conflict.

## Fixed SQL, bound values

```csharp
private const string DuplicateTitleMessage = "A Todo with this title already exists for this owner.";

private static readonly string SelectByIdSql =
    $"SELECT {TodoColumns} FROM \"{TodoTable.Name}\" WHERE \"{TodoTable.IdColumn}\" = @id;";

private static readonly string InsertSql = $"""
    INSERT INTO "{TodoTable.Name}" ({TodoColumns})
    VALUES (@id, @title, @description, @createdAt, @updatedAt, @status, @createdBy, @updatedBy);
    """;
```

Rules:

- Identifiers are compile-time constants. Values are `command.Parameters.Add(name, NpgsqlDbType.X).Value`.
- IN lists: one parameter per element (`@p0, @p1, …`), never string concatenation. See
  `AppendInCondition`.
- Contains: `ILIKE @p ESCAPE '\'` with `%…%`, and escape `\`, `%` and `_` in the input.
- Date range: both bounds or none; `timestamp with time zone` values go through `ToUtc` (Unspecified is
  treated as UTC, Local is converted). Npgsql refuses a non-UTC `DateTime` for `timestamptz`.
- Paging: `LIMIT @pageSize OFFSET @offset` only when both values are present; below 1 throws
  `ArgumentOutOfRangeException` (the validator already rejects that).
- Sorting: resolve the key through `{Feature}SortColumns.Map` (case-insensitive) and emit the column plus
  `ASC`/`DESC` from the boolean. A blank key or an unknown key throws `ArgumentException`.

## Mapping

`Map{Feature}` reads by ordinal and checks `IsDBNull` on every column:

```csharp
private static TodoEntity MapTodo(NpgsqlDataReader reader) => new()
{
    Id = reader.IsDBNull(0) ? Ulid.Empty : new Ulid(reader.GetFieldValue<byte[]>(0)),
    Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
    CreatedAt = reader.IsDBNull(3) ? default : reader.GetDateTime(3),
    Status = reader.IsDBNull(5) ? ETodoStatus.Pending : (ETodoStatus)reader.GetInt32(5),
    // …
};
```

- `Ulid` is stored as `bytea` (16 bytes): `Ulid.ToByteArray()` on write, `new Ulid(byte[])` on read.
- `ETodoStatus` is stored as `integer`: `(int)entity.Status` on write, `(ETodoStatus)reader.GetInt32(i)` on
  read.
- `timestamptz` maps to `DateTime` with `Kind = Utc`.
- There is no reflection and no convention-based mapping: AOT and explicitness require the manual map.

## Unique index and conflicts

```csharp
catch (PostgresException exception) when (IsDuplicateTitle(exception))
{
    return Result<TodoEntity>.Failure(new ConflictError(DuplicateTitleMessage));
}

private static bool IsDuplicateTitle(PostgresException exception) =>
    exception.SqlState == PostgresErrorCodes.UniqueViolation
    && exception.ConstraintName == TodoTable.UniqueTitleIndexName;
```

The `ConstraintName` check matters: without it, a primary-key collision would be reported as a duplicate
title. The handler still pre-checks uniqueness to return a friendly conflict; the index protects the race.
Do not catch `PostgresException` broadly and do not use exceptions for expected outcomes elsewhere.

## Sort catalog and sort columns

Application owns the accepted vocabulary, Infrastructure the physical map:

```csharp
// Application/Todo/TodoSortCatalog.cs
public static readonly IReadOnlyList<string> Keys = [nameof(TodoDto.Id), …, nameof(TodoDto.UpdatedBy)];
public static IReadOnlySet<string> KeySet { get; } = new HashSet<string>(Keys, StringComparer.OrdinalIgnoreCase);
public static string Describe() => string.Join(", ", Keys.Select(key => char.ToLowerInvariant(key[0]) + key[1..]));

// Infrastructure/Todo/TodoSortColumns.cs
public static readonly IReadOnlyDictionary<string, string> Map =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(TodoEntity.Id)] = TodoTable.IdColumn,
        // …
    };
```

`TodoSortColumnsTests` (unit) asserts both name the same keys, case-insensitively. The validator and the
OpenAPI description read `TodoSortCatalog`; the repository reads `TodoSortColumns`.

## Projections

A partial read is a positional record (`TodoTitle(Ulid Id, string Title)`) returned by a dedicated port
method. Its SQL selects only the needed columns and maps them explicitly. Do not return the entity and hide
fields in the response; the projection is the contract.

## SQL scripts

`local/database/*.sql` (schema) and `local/database/seed/*.sql` (seed) are applied by
`CleanWebApiTemplate.DbInitializer` in **ordinal order**, one transaction per file.

- Names: `0001__initial_schema.sql`, `0002__projects_schema.sql`, `0001__todos.sql`, `0002__projects.sql`.
- Schema scripts use `IF NOT EXISTS` and are safe to re-run.
- Forward-only: never edit a deployed script. A change to an existing table is a new higher-numbered script.
- The local database is ephemeral (no volume): every `aspire start` recreates it from scratch.
- `db-init` exits 0 when at least one table exists in `public`; it does not name a feature table.
- The seed runs only when the AppHost passes `Seed__Path`, which happens in Local mode only. Tests start from
  the schema with no rows.
- Seed rows use fixed ULID bytes (`decode('018f…', 'hex')`) and `ON CONFLICT ("Id") DO NOTHING`, so a script
  can be re-applied.

## Transactions

There is no unit of work and no shared connection. Each port method opens and closes its connection. A use
case that must change several rows atomically adds an explicit port method that performs the statements in one
transaction inside the repository:

```csharp
await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
// commands with command.Transaction = transaction
await transaction.CommitAsync(ct);
```

Do not leak a transaction, a connection or a command outside Infrastructure.

## Test data access

`tests/CleanWebApiTemplate.Tests.Shared/Data/{Feature}TestRepository.cs` wraps the real repository over its
own `NpgsqlDataSource`:

```csharp
public sealed class TodoTestRepository : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITodoRepository _repository;

    public TodoTestRepository(string connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _repository = new TodoRepository(_dataSource);
    }

    public Task<TodoEntity?> FindAsync(Ulid id) =>
        _repository.GetByIdAsync(id, cancellationToken: TestCancellation.Token);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
```

Tests arrange with `AddAsync`/builders and assert with `FindAsync`/`FindByTitleAsync`/`CountAsync`. They
never issue raw SQL and never hardcode ids. Respawn reads the table graph after the schema exists, so a new
table is reset automatically; a new database gets a new reset name in `DatabaseResetConfiguration`.
