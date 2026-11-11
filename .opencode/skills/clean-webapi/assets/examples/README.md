# Worked example: the `Project` feature

A second complete feature, mirroring the template's layout. Use it when the `Todo` sample is gone, has been
modified, or when you need an independent reference. Every file uses `CleanWebApiTemplate` names, so
`dotnet new` rewrites them like the rest of the template.

## What it contains

| Path | Content |
|---|---|
| `src/CleanWebApiTemplate.Domain/Project` | `ProjectEntity`, `EProjectStatus` (Draft, Active, Completed, Archived), `ProjectConstraints` |
| `src/CleanWebApiTemplate.Application/Project` | Port, DTO, mapping, filter, name projection, sort catalog, shared validator, and one folder per use case (create, get by id, filter, update, delete, names) |
| `src/CleanWebApiTemplate.Infrastructure/Project` | `ProjectTable`, `ProjectSortColumns`, `ProjectRepository` |
| `src/CleanWebApiTemplate.Api/Project` | Module, routes, endpoint adapter, requests, responses, JSON context, OpenAPI schemas |
| `tests/CleanWebApiTemplate.UnitTests` | Handler and validator tests, sort alignment, request mapping, JSON contract |
| `tests/CleanWebApiTemplate.Api.ComponentTests/Project` | Endpoints, filter, ownership, authorization matrix, repository, validation |
| `tests/CleanWebApiTemplate.Api.FunctionalTests/Project` | Lifecycle flow, authentication |
| `tests/CleanWebApiTemplate.Tests.Shared/Data` | `ProjectBuilder`, `ProjectTestRepository` |
| `local/database` | `0002__projects_schema.sql`, `seed/0002__projects.sql` |
| `local/openapi` | `examples.json` entries and the `bodies/` and `responses/` files |

## How to copy it

1. Copy the three trees over the repository root, keeping the paths:

   ```bash
   cp -r assets/examples/src/.  <repo>/src/
   cp -r assets/examples/tests/. <repo>/tests/
   cp -r assets/examples/local/. <repo>/local/
   ```

2. Apply the four registrations in `FixtureRegistration.md`.
3. Add `services.AddProjectModule();` to `Api/ConfigureServices.cs` (next to `AddTodoModule()`).
4. Merge the `operations` object of `local/openapi/examples.json` into the repository catalog. If the
   repository still has the Todo sample, both features share the catalog.
5. Update `tests/CleanWebApiTemplate.Api.ComponentTests/OpenApiContractTests.cs`: the operation-id set and the
   per-path status assertions are exact, so a new feature makes `Document_ContainsEveryTodoOperation` fail
   until the expected list includes the new operations.
6. Build, run the tests and start Aspire (`aspire start`) to try the examples.

## Notes

- The tests use the template's role identities (`TestUser.TodoUser`, `TodoOperator`, `TodoAdmin`,
  `TodoExternal`). They are role-based, not feature-based; rename them in `TestUser`/`TestUsers` only if the
  whole repository renames the sample identities.
- `Fixture.Projects` is added by `FixtureRegistration.md`; until then the component tests do not compile.
- The unique index is `UX_Project_CreatedBy_Name` and the repository only maps **that** constraint to a
  conflict.
- The name is unique per owner: two owners may use the same name.
- The filter exposes `names`, `statuses` and `owners`; the projection route is `POST /api/projects/names`.
