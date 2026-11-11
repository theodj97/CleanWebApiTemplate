# Fixture registration

Add the test repository to `Tests.Shared/Fixtures/TestServerFixtureBase.cs` after the connection string is
known. The component and functional fixtures inherit it; no change to either fixture is needed for a new table
in the same database.

```csharp
using {App}.Tests.Shared.Data;

public abstract class TestServerFixtureBase : IAsyncLifetime
{
    // ... existing properties ...

    /// <summary>Repository the tests use to arrange and assert Project rows.</summary>
    public ProjectTestRepository Projects { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // ... existing startup ...

        var connectionString = await RequiredConnectionStringAsync(Resources.PostgresDb, cancellationToken);
        Todos = new TodoTestRepository(connectionString);
        Projects = new ProjectTestRepository(connectionString);

        // ... existing application start and resetter registration ...
    }
}
```

If the sample `Todo` is removed, remove `Todos` and `TodoTestRepository` in the same change.

## Database resetters

A new **table** in the existing database needs no new resetter: `RespawnDatabaseResetter` reads the table graph
after the schema exists and resets every table. A new **database** needs:

```csharp
public static class Names
{
    public const string Todo = "TodoDb";
    public const string Project = "ProjectDb";
}
```

and a `Respawner` registered by the fixture for that connection string. `[ResetDatabase("ProjectDb")]` then
resolves by name.

`TablesToIgnore` is only for tables the tests never write (migration history, reference data seeded with the
schema). A table the tests write is reset like any other.
