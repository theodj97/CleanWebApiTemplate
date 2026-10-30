# 12 — Batch 4a: estructura repository-centric y Result sin HTTP

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Parte de Batch 4 acordada con el usuario:

- estructura repository-centric;
- `TodoConstraints` en Domain y `TodoTable` solo con metadata SQL;
- `AppSettings`/`Constants` fuera de Domain;
- quitar `Microsoft.AspNetCore.Http*` de Domain;
- separar `Result<T>` de la semántica HTTP y moverlo a Application.

El resto de Batch 4 (ArchUnitNET, `BannedApiAnalyzers`, `AppConfigurationHelper`,
`BaseApiRouter`, etc.) queda pendiente.

## Árbol resultante

```text
src/
├── Application/
│   ├── Results/
│   │   ├── Result.cs
│   │   ├── Error.cs
│   │   └── IResultFactory.cs
│   └── Todo/
│       ├── Abstractions/ITodoRepository.cs
│       └── Queries/TodoFilter.cs
├── Domain/
│   └── Models/Todo/TodoConstraints.cs
├── Infrastructure/
│   ├── Data/NpgsqlConnectionFactory.cs
│   └── Repositories/Todo/
│       ├── TodoRepository.cs
│       └── Helpers/
│           ├── TodoTable.cs
│           └── TodoSortColumns.cs
└── Api/
    └── Configuration/
        ├── AppSettings.cs
        ├── ConnectionStringsSection.cs
        ├── SectionBase.cs
        └── Constants.cs
```

## Cambios

### Repository-centric

- `ITodoRepository` y `TodoFilter` viven en `Application`.
- `TodoRepository`, `TodoTable` y `TodoSortColumns` viven en
  `Infrastructure/Repositories/Todo`; los helpers SQL quedan pegados a su repositorio.
- `NpgsqlConnectionFactory` sigue en `Infrastructure/Data`.
- `Application` ya no referencia `Infrastructure`; `Infrastructure` referencia `Application`.

### Domain limpio

- `TodoConstraints` contiene las longitudes de negocio.
- `TodoTable` solo contiene nombres de tabla/columnas e índice único.
- `AppSettings`, `ConnectionStringsSection`, `SectionBase` y `Constants` se movieron a
  `Api/Configuration`.
- `Domain.csproj` ya no referencia `Microsoft.AspNetCore.Http` ni `.Abstractions`.

### Result<T> sin HTTP

- `Result<T>` se movió a `Application/Results`.
- Solo conserva `IsSuccess`, `IsFailure`, `Error` y `Value`.
- Se eliminaron `IsCreated`, `IsNoContent`, `Created(...)` y la detección de colecciones vacías.
- `ApiResultExtensions` decide:
  - 200 para un valor;
  - 204 para `null` o colección vacía;
  - 201 mediante `ToCreatedResponse(...)`;
  - 4xx/5xx para errores.
- `CreateTodoService` devuelve `Success`; `TodoRoutes.CreateTodo` usa `ToCreatedResponse`.

## Tests

| Proyecto | Tests |
|---|---:|
| `Api.UnitTests` | 135 |
| `Api.ComponentTests` | 72 |
| `Api.FunctionalTests` | 11 |
| `OpenApiExamples.UnitTests` | 33 |
| **Total** | **251** |

Los tests de `Result<T>` se redujeron a sus invariantes; los 200/201/204 se cubren en
`ApiResultExtensionsTests` y en los tests de componente.

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ 0 warnings AOT |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 251 tests, 0 failed |
| `dotnet format whitespace/style` | ✅ |
| `dotnet format analyzers` (excluyendo IL2026/IL3050) | ✅ |
| Smoke test AOT | ✅ 201 válido, 400 null/missing, 403 sin email, 401 HS512 |

Comprobaciones de arquitectura:

```text
Application -> Infrastructure: sin referencias
Domain -> Microsoft.AspNetCore: sin referencias
Api -> Domain.Configuration: sin referencias
Infrastructure/Repositories: solo TodoRepository + Helpers
Result<T>: sin IsCreated/IsNoContent/Created
```

## Pendiente

- **Batch 4b**: ArchUnitNET, `BannedApiAnalyzers`, `AppConfigurationHelper`,
  `BaseApiRouter`, `OpenApiConfiguration`.
- **Batch 5**: cobertura de caminos críticos.
- **Batch 6**: hooks y guardarraíles.
- **Batch 7**: template y readmes.
