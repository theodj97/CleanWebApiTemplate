# Registration points for the example

The `Project` example adds files; these four edits wire it into the shared code. Apply them after copying the
trees.

## 1. `src/CleanWebApiTemplate.Api/ConfigureServices.cs`

```csharp
services.AddPostgres(appSettings.ConnectionStrings.Postgres);
services.AddTodoModule();       // existing
services.AddProjectModule();    // add this line and the using
```

Add `using CleanWebApiTemplate.Api.Project;`.

## 2. `tests/CleanWebApiTemplate.Tests.Shared/Fixtures/TestServerFixtureBase.cs`

```csharp
public TodoTestRepository Todos { get; private set; } = null!;
public ProjectTestRepository Projects { get; private set; } = null!;

// in InitializeAsync, after the connection string is known:
Todos = new TodoTestRepository(connectionString);
ProjectTestRepositoryIsCreated = true;
Projects = new ProjectTestRepository(connectionString);

// in DisposeAsync, before the AppHost is disposed:
if (ProjectTestRepositoryIsCreated)
{
    await Projects.DisposeAsync();
}
```

Add `private bool ProjectTestRepositoryIsCreated { get; set; }`. If the Todo sample is gone, ignore the Todo
lines and adjust the flag name.

## 3. `local/openapi/examples.json`

Merge the `operations` object of the example's `examples.json` into the repository catalog. The keys must
match the document (`POST /api/projects`, `GET /api/projects/{id}`, …); `parameters` stays an array.

## 4. `tests/CleanWebApiTemplate.Api.ComponentTests/OpenApiContractTests.cs`

The document test compares the exact operation-id set; add the Project operations (`CreateProject`,
`GetProjectById`, `FilterProjects`, `GetProjectNames`, `UpdateProject`, `DeleteProject`) and the `/api/projects`
path assertions.

## 5. Nothing else

- `DatabaseResetConfiguration` needs no change: Respawn resets every table in `public` after the schema
  exists, and the example reuses the existing logical database name.
- `TestUser`/`TestUsers` need no change: the identities are role-based.
- `EndpointExtension` needs no change: the module registers `ProjectRoutes` as an `IGroupMap`.
- `AppJsonSerializerContext` needs no change: the feature has its own `ProjectJsonSerializerContext`.
- `OpenApiConfiguration` needs no change: the module adds its own document transformer.

## Verify

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet test --project tests/CleanWebApiTemplate.UnitTests/CleanWebApiTemplate.UnitTests.csproj
dotnet test --project tests/CleanWebApiTemplate.Api.ComponentTests/CleanWebApiTemplate.Api.ComponentTests.csproj
aspire start   # the swagger resource lists the Project operations
```
