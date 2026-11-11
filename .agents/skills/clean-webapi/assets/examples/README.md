# Worked example: Project

A complete second feature, independent of the template's `Todo` sample. It demonstrates every pattern a feature
needs: create, read by id and filter, with validation, ownership, a unique name per owner, an ADO.NET
repository, a schema script, a seed and tests at every level.

`{App}` is the root namespace. Replace it with the solution name (`Contoso.Orders`) and `Project` with the real
feature. The example assumes the template's shared types exist:

- `BaseEntity<TKey>` in Domain.
- `IHandler<TInput, TResult>`, `BaseAbstractValidator<T>`, `ValidationService`, `ValidatorSet<T>`,
  `Result<T>`, `Error` and the error records in Application.
- `ICurrentUser` and `OwnerScope` in `Application/Abstractions/Identity`.
- `IBaseResponse<TDto, TResponse>` in Api.
- The test fixtures, attributes, builders, `TestData`, `Eventually` and the test repository in `Tests.Shared`.

Do not copy the `Todo` feature into the generated project. Copy these files in layer order and adapt the
names. The destination paths are the ones in `../../references/feature-slice.md`; for example,
`application/ProjectDto.cs` → `src/{App}.Application/Dtos/Project/ProjectDto.cs`,
`application/CreateProjectHandler.cs` →
`src/{App}.Application/Project/Handlers/Create/CreateProjectHandler.cs` and
`infrastructure/ProjectRepository.cs` → `src/{App}.Infrastructure/Repositories/Project/ProjectRepository.cs`.

The tests use the template's role identities (`TodoUser`, `TodoOperator`, `TodoExternal`). If the sample is
removed and those identities are renamed, use the role identities that exist; keep one identity per role and
the two negative cases.

## Files

```text
assets/examples/
├── domain/
│   ├── ProjectEntity.cs
│   ├── EProjectStatus.cs
│   └── ProjectConstraints.cs
├── application/
│   ├── Abstractions/
│   │   └── Repositories/
│   │       └── IProjectRepository.cs
│   ├── ProjectDto.cs
│   ├── ProjectDtoExtensions.cs
│   ├── ProjectValidator.cs
│   ├── ProjectFilter.cs
│   ├── ProjectSortCatalog.cs
│   ├── CreateProjectInput.cs
│   ├── CreateProjectInputValidator.cs
│   ├── CreateProjectHandler.cs
│   ├── GetProjectByIdInput.cs
│   ├── GetProjectByIdInputValidator.cs
│   ├── GetProjectByIdHandler.cs
│   ├── FilteredProjectInput.cs
│   ├── FilteredProjectInputValidator.cs
│   └── FilteredProjectHandler.cs
├── infrastructure/
│   ├── ProjectTable.cs
│   ├── ProjectSortColumns.cs
│   └── ProjectRepository.cs
├── api/
│   ├── CreateProjectRequest.cs
│   ├── FilteredProjectRequest.cs
│   ├── ProjectResponse.cs
│   ├── ProjectOpenApiSchemas.cs
│   ├── ProjectEndpoints.cs
│   ├── ProjectRoutes.cs
│   └── registration.md
├── database/
│   ├── 0002__projects.sql
│   └── seed/0002__projects.sql
├── local/
│   └── openapi-examples.md
└── tests/
    ├── CreateProjectInputValidatorTests.cs
    ├── CreateProjectHandlerTests.cs
    ├── ProjectSortColumnsTests.cs
    ├── ProjectBuilder.cs
    ├── ProjectTestRepository.cs
    ├── FixtureRegistration.md
    ├── ProjectEndpointsTests.cs
    ├── ProjectAuthorizationMatrixTests.cs
    ├── ProjectLifecycleFlowTests.cs
    ├── ProjectResponseDto.cs
    └── ProjectArchitectureTests.md
```

## What is intentionally omitted

- Update and delete. They follow the same shape as create/get: `UpdateProjectInput` + validator + handler,
  `DeleteProjectInput` + validator + handler, `PatchFields`, an owner-scoped repository update/delete and the
  `Operator` policy for delete. The touch list in `../../references/feature-slice.md` covers them.
- The titles/projection endpoint. It is the same read path with a projection type in
  `Application/{Feature}/Projections` (for example `ProjectTitle`) and a `ProjectTitleResponse`. Do not fill a
  partial `ProjectDto` and cast it later.
- A new AppHost resource. A table in the existing database needs no AppHost change; only `db-init`'s readiness
  query must keep referencing a table that exists.

## Copy order

1. Domain.
2. Application (input → validator → port/filter → sort catalog → DTO → handler).
3. Infrastructure (table → sort columns → repository).
4. Database (schema → seed).
5. API (request/response → endpoint adapter → schemas → route group → registration).
6. Local (OpenAPI examples).
7. Tests (builder/repository → fixture → unit → sort alignment → component → functional → architecture).
   Update `ComponentTests/OpenApiContractTests.cs` too: it asserts the exact operation-id set.
8. Docs (README API table) and gates.
