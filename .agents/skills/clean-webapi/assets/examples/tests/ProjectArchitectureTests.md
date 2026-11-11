# Architecture tests for the example

The existing `DependencyRulesTests` rules are namespace/assembly based and already cover a new feature. Add an
assertion to `StructureTests` for the new port and filter, so a future refactor cannot move them out of
Application:

```csharp
[Fact]
public void ProjectPortAndQueryContract_ShouldLiveInApplication()
{
    Assert.Equal("{App}.Application.Abstractions.Repositories", typeof(IProjectRepository).Namespace);
    Assert.Equal("{App}.Application.Project.Handlers.Queries", typeof(ProjectFilter).Namespace);
}
```

Add the using directives the test needs:

```csharp
using {App}.Application.Abstractions.Repositories;
using {App}.Application.Project.Handlers.Queries;
```

## Generalize the helper condition

The template's `StructureTests.InfrastructureRepositories_ShouldContainOnlyRepositoriesAndTheirHelpers`
currently hardcodes `Infrastructure.Repositories.Todo.Helpers`:

```csharp
// Before: only the sample feature's helpers are accepted.
|| type.Namespace!.StartsWith("{App}.Infrastructure.Repositories.Todo.Helpers",
                              StringComparison.Ordinal),
```

Adding `ProjectTable` and `ProjectSortColumns` under `Repositories.Project.Helpers` makes that test fail. The
`ProjectSortCatalog` is in Application, so it is not part of this condition. Replace the hardcoded prefix with
a namespace check, so every feature's `Helpers` folder is accepted:

```csharp
using {App}.Infrastructure.Repositories.Project;

[Fact]
public void InfrastructureRepositories_ShouldContainOnlyRepositoriesAndTheirHelpers()
{
    var types = typeof(ProjectRepository).Assembly.GetTypes()
        .Where(type => type.Namespace?.StartsWith("{App}.Infrastructure.Repositories",
                                                  StringComparison.Ordinal) == true)
        .Where(type => type.IsClass && !type.IsNested)
        .ToArray();

    Assert.NotEmpty(types);
    Assert.All(types, type => Assert.True(
        type.Name.EndsWith("Repository", StringComparison.Ordinal)
        || type.Namespace!.Contains(".Helpers", StringComparison.Ordinal),
        $"{type.FullName} is not a repository or a repository helper."));
}
```

Keep the assertion: it is what stops `Infrastructure/Repositories` from becoming a dumping ground. Generalize
the namespace, not the rule.

## Sort catalog alignment

`ProjectSortCatalog` (Application) and `ProjectSortColumns` (Infrastructure) are two views of the same
vocabulary. The unit test `ProjectSortColumnsTests` asserts
`ProjectSortCatalog.KeySet.SetEquals(ProjectSortColumns.Map.Keys)` and that every key resolves
case-insensitively. Keep it next to the existing helper tests; it is not an architecture test, but it protects
the same "no drift" property.

## New projects and namespaces

When the feature adds a **new project** (not just a new feature), add its assembly to the `ArchLoader` list in
`DependencyRulesTests` and a rule for its allowed dependencies. The template's five projects are all loaded
today.

When the sample `Todo` is removed, keep the architecture tests generic: replace the `Todo` type references with
the remaining features, and do not leave a test that asserts a type that no longer exists.
