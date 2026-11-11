# Registration snippets

A feature that is not registered is invisible. Add every entry below in the same change.

## `Api/ConfigureServices.cs`

```csharp
private static IServiceCollection ConfigureServicesAndValidators(this IServiceCollection services)
{
    // Validators
    services.AddTransient<IValidator<CreateProjectInput>, CreateProjectInputValidator>();
    services.AddTransient<IValidator<GetProjectByIdInput>, GetProjectByIdInputValidator>();
    services.AddTransient<IValidator<FilteredProjectInput>, FilteredProjectInputValidator>();

    // One set per input: an empty set is a startup failure, not a silent validation bypass.
    services.AddTransient<ValidatorSet<CreateProjectInput>>();
    services.AddTransient<ValidatorSet<GetProjectByIdInput>>();
    services.AddTransient<ValidatorSet<FilteredProjectInput>>();

    // Handlers
    services.AddTransient<IHandler<CreateProjectInput, ProjectDto?>, CreateProjectHandler>();
    services.AddTransient<IHandler<GetProjectByIdInput, ProjectDto?>, GetProjectByIdHandler>();
    services.AddTransient<IHandler<FilteredProjectInput, IEnumerable<ProjectDto?>>, FilteredProjectHandler>();

    return services;
}

private static IServiceCollection AddPersistence(this IServiceCollection services,
                                                 ConnectionStringsSection connectionStrings)
{
    services.AddSingleton<INpgsqlConnectionFactory>(new NpgsqlConnectionFactory(connectionStrings.Postgres));
    services.AddScoped<IProjectRepository, ProjectRepository>();

    return services;
}
```

## `Api/Program.cs`

`ValidateOnBuild` turns a missing `ValidatorSet<T>` (or a set with no validator) into a startup failure:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

## `Api/Extensions/EndpointExtension.cs`

```csharp
public static IServiceCollection RegisterMinimalEndpoints(this IServiceCollection services)
{
    services.TryAddEnumerable(ServiceDescriptor.Transient<IGroupMap, TodoRoutes>());
    services.TryAddEnumerable(ServiceDescriptor.Transient<IGroupMap, ProjectRoutes>());

    return services;
}
```

## `Api/Configuration/AppJsonSerializerContext.cs`

```csharp
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(FilteredProjectRequest))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(IEnumerable<ProjectResponse>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

Keep the existing entries; add the new ones. A collection response needs both the item type and the
`IEnumerable<T>` entry.

## `Api/Configuration/OpenApiConfiguration.cs`

```csharp
private const string ProjectTag = "Project";

document.Tags = new HashSet<OpenApiTag>
{
    new()
    {
        Name = TodoTag,
        Description = "...",
    },
    new()
    {
        Name = ProjectTag,
        Description =
            "Create, read and filter projects. Every operation requires a JWT; ownership and role "
            + "rules are listed on each operation.",
    },
};

CommonOpenApiSchemas.DescribeSchemas(document);
TodoOpenApiSchemas.DescribeSchemas(document);
ProjectOpenApiSchemas.DescribeSchemas(document);
```

If the feature replaces the sample, remove the `TodoTag` and the `TodoOpenApiSchemas` call in the same change.

## `README` API table

```markdown
| Method | Route | Policy | Success |
|---|---|---|---|
| `GET` | `/api/Project/{id}` | User | `200`; `204` when missing or not owned |
| `POST` | `/api/Project/filter` | User | `200`; `204` when empty |
| `POST` | `/api/Project/` | User | `201` with `Location`; `409` for a duplicate name |
```

## Test registration

If the feature adds a new test repository, register it on `Tests.Shared/Fixtures/TestServerFixtureBase.cs`:

```csharp
public ProjectTestRepository Projects { get; private set; } = null!;

// after the connection string is known:
var connectionString = await RequiredConnectionStringAsync(Resources.PostgresDb, cancellationToken);
Projects = new ProjectTestRepository(connectionString);
```

A new table in the same database needs no new resetter name. A new **database** needs an entry in
`DatabaseResetConfiguration.Names` and a resetter registered by the fixture.

## OpenAPI contract test

`ComponentTests/OpenApiContractTests.cs` asserts the **exact** operation-id set and the status codes of every
path. A new endpoint makes the suite fail until its ids and paths are added, which is the point: the document is
part of the contract.

Add the Project ids (keeping the list sorted) and the Project paths:

```csharp
Assert.Equal(
    ["CreateProject", "CreateTodo", "DeleteTodo", "FilterProjects", "FilterTodos",
     "GetProjectById", "GetTodoById", "GetTodoTitles", "UpdateTodo"],
    operationIds);

AssertResponses(paths.GetProperty("/api/Project").GetProperty("post"),
                "201", "400", "401", "403", "409", "500");
AssertResponses(paths.GetProperty("/api/Project/{id}").GetProperty("get"),
                "200", "204", "400", "401", "403", "500");
AssertResponses(paths.GetProperty("/api/Project/filter").GetProperty("post"),
                "200", "204", "400", "401", "403", "500");
```

Also add `Assert.Contains("Project", tags);` to `Document_DescribesTheApiAndTheBearerScheme` and the Project
schema assertions to `Schemas_KeepTheValidationConstraints` (name max length, ULID pattern, status enum).

If the feature replaces the sample, change the expected set to the operations that remain; do not delete the
test.
