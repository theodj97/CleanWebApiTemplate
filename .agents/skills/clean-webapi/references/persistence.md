# Persistence

PostgreSQL through Npgsql and plain ADO.NET. There is no EF Core, no Dapper and no reflection-based mapping;
that is a deliberate Native AOT decision, not a gap to fill.

## Schema source of truth

- `local/database/*.sql` is the schema. `db-init` applies every file in ordinal order, each in a transaction,
  on every AppHost start. There are no migrations and no history table.
- `local/database/seed/*.sql` is the Local-mode seed. Tests start from the schema only and insert their own
  data.
- The database is ephemeral: the container is removed when the AppHost stops. Never add a volume, a bind mount
  or a persistent lifetime.
- The application never creates or migrates the schema. A deployed environment owns its schema.

Script naming:

```text
local/database/0001__initial_schema.sql
local/database/0002__projects.sql
local/database/seed/0001__todos.sql
local/database/seed/0002__projects.sql
```

`DbInitializer` orders by `StringComparer.Ordinal`, so zero-pad the number. It fails when the schema directory
has no `*.sql`; a seed directory with no files is skipped.

## Table design

Use the shape the template establishes:

```sql
CREATE TABLE IF NOT EXISTS "Project" (
    "Id"          bytea       NOT NULL,
    "Name"        text        NOT NULL,
    "Description" text        NULL,
    "CreatedAt"   timestamptz NOT NULL,
    "UpdatedAt"   timestamptz NOT NULL,
    "Status"      integer     NOT NULL,
    "CreatedBy"   text        NOT NULL,
    "UpdatedBy"   text        NOT NULL,
    CONSTRAINT "PK_Project" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Project_CreatedBy_Name"
    ON "Project" ("CreatedBy", "Name");

CREATE INDEX IF NOT EXISTS "IX_Project_CreatedAt" ON "Project" ("CreatedAt");
```

- `Id bytea` holds a 16-byte ULID (`Ulid.ToByteArray()` / `new Ulid(bytes)`). Do not use `uuid` or `text`.
- Timestamps are `timestamptz` and always written as UTC.
- The status is `integer`; the application enum is the source of the allowed values.
- The unique index encodes the business rule the handler also pre-checks; it closes the race between the
  pre-check and the insert.
- Name indexes after the rule: `UX_{Table}_{Columns}`, `IX_{Table}_{Column}`, `PK_{Table}`.

## Table and sort metadata

`Infrastructure/Repositories/{Feature}/Helpers/{Feature}Table.cs`:

```csharp
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

    public const string UniqueNameIndexName = "UX_Project_CreatedBy_Name";
}
```

`Application/{Feature}/Sorting/{Feature}SortCatalog.cs` owns the **accepted sort keys** and the description
used by OpenAPI:

```csharp
public static class ProjectSortCatalog
{
    public static readonly IReadOnlyList<string> Keys =
        [nameof(ProjectDto.Id), nameof(ProjectDto.Name), nameof(ProjectDto.Description),
         nameof(ProjectDto.CreatedAt), nameof(ProjectDto.UpdatedAt), nameof(ProjectDto.Status),
         nameof(ProjectDto.CreatedBy), nameof(ProjectDto.UpdatedBy)];

    public static IReadOnlySet<string> KeySet { get; } =
        new HashSet<string>(Keys, StringComparer.OrdinalIgnoreCase);

    public static string Describe() =>
        string.Join(", ", Keys.Select(key => char.ToLowerInvariant(key[0]) + key[1..]));
}
```

`Infrastructure/Repositories/{Feature}/Helpers/{Feature}SortColumns.cs` maps the same keys to the physical
columns with an ordinal-ignore-case dictionary. Only keys in this map can reach `ORDER BY`; the direction is a
`bool`, never raw text. `{Feature}SortColumnsTests` asserts
`{Feature}SortCatalog.KeySet.SetEquals({Feature}SortColumns.Map.Keys)`, so the validator's friendly `400` and
the SQL column map cannot drift. A new sortable field edits the catalog and the map, then fails the test until
both agree.

## Repository rules

- One connection per operation: `using var connection = connectionFactory.CreateConnection(); await
  connection.OpenAsync(cancellationToken);`.
- Fixed SQL text is built once from `{Feature}Table` constants (static readonly fields). Values are always
  `NpgsqlParameter`s with an explicit `NpgsqlDbType`.
- Dynamic filters append `column IN (@p0, @p1, ...)` with one parameter per value. The column name comes from a
  constant, never from input.
- Text search uses `ILIKE @p ESCAPE '\'` and escapes `\`, `%` and `_`.
- The date range applies only when both bounds are present; convert to UTC (`DateTime.SpecifyKind`/`ToUniversalTime`)
  before sending a `TimestampTz`.
- Paging appends `LIMIT @pageSize OFFSET @offset`; validate page number/size before calling.
- Sorting resolves through `{Feature}SortColumns` after the validator accepts the key through
  `{Feature}SortCatalog`; unknown keys throw `ArgumentException`, which the validator should have caught.
- Row mapping is manual, in the exact `{Feature}Columns` order, with `IsDBNull` checks for every nullable
  column.
- Return `Result<T>` for a constraint violation that is a business outcome; return `null`/`false` for a row
  that does not exist.

Unique violation:

```csharp
catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
{
    return Result<ProjectEntity>.Failure(
        new ConflictError("A project with this name already exists for this owner."));
}
```

Do not catch `PostgresException` broadly; a connection failure or a syntax error is not a conflict.

## Query filter

`Application/{Feature}/Handlers/Queries/{Feature}Filter.cs` is the persistence-neutral query shape:

```csharp
public sealed record ProjectFilter
{
    public IReadOnlyList<Ulid>? Ids { get; init; }
    public IReadOnlyList<string>? Names { get; init; }
    public IReadOnlyList<int>? Statuses { get; init; }
    public IReadOnlyList<string>? CreatedBys { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }
}
```

The API request uses `string[]`/`string?`; `FilteredProjectInput.ToFilter()` parses ULIDs and dates with
`CultureInfo.InvariantCulture` and `DateTimeStyles.RoundtripKind`.

## Seed

- The seed is synthetic and local-only. It must make every endpoint work with the devtoken profiles.
- Use the same emails as `local/tools/devtoken.json` and `local/openapi/users.json`; roles and ownership must
  match.
- Cover every state the endpoints accept (for example one Draft, one Active, one Completed).
- Keep ids stable and readable: `decode('018f0c2a000000000000000000000001', 'hex')` is a valid 16-byte ULID
  payload.
- Do not insert data that violates a unique index; the seed runs on every start, not once.
- Document the seed coverage in `local/local-development.md` when the feature has non-obvious states.

## Test data

- `{Feature}Builder` in `Tests.Shared/Data` supplies valid defaults and states only what the test cares about.
  Use `TestData.Unique("project")` for unique columns and `TestData.CreateFaker()` for deterministic random
  values.
- `{Feature}TestRepository` wraps the real repository over the test connection string:
  `AddAsync` (throws on failure), `TryAddAsync` (returns the result for conflict assertions), `FindAsync`,
  `FindByNameAsync`, `CountAsync`.
- Register it on the fixture after the connection string is known:
  `Projects = new ProjectTestRepository(connectionString);`.
- A new table in the same database needs no new resetter name. `RespawnDatabaseResetter` reads the table graph
  after the schema exists and resets every table. Add a name to `DatabaseResetConfiguration.Names` only for a
  new **database**.
- `DatabaseResetConfiguration.TablesToIgnore` is for tables nobody writes during tests (migration history,
  reference data seeded with the schema). A table the tests write to is reset like any other.
- `[ResetDatabase]` runs **before** the test, so a failed test leaves its data for inspection.

## Connection factory

`INpgsqlConnectionFactory` is registered as a singleton with the connection string from
`AppSettings.ConnectionStrings.Postgres`. Npgsql pools the underlying connections per connection string.
Repositories are scoped; they take the factory, not the connection string.

## Checklist

- [ ] Schema script applies from an empty database and is idempotent enough for a fresh container.
- [ ] Unique index name matches `{Feature}Table.Unique...IndexName`.
- [ ] Every query is parameterised; table/column names come from constants.
- [ ] Sort catalog and column map match; a unit test proves it.
- [ ] Row mapping covers every column with `IsDBNull`.
- [ ] ULIDs are `bytea`; timestamps are UTC `timestamptz`.
- [ ] Unique violation maps to `ConflictError`, not `500`.
- [ ] Seed covers the states and uses the devtoken emails/roles.
- [ ] Test repository and builder exist; fixture property registered.
- [ ] `db-init` readiness query still references a table that exists.
