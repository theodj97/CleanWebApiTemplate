# 13 — Batch 4b: guardarraíles, configuración, usuario actual y OpenAPI

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Segunda parte de Batch 4, acordada con el usuario:

- tests de arquitectura con ArchUnitNET;
- `BannedApiAnalyzers` + `BannedSymbols.txt` en `src/`;
- `AppConfigurationHelper` determinista y con secretos de desarrollo solo en Development;
- `ICurrentUser` y eliminación de `BaseApiRouter`;
- split de `OpenApiConfiguration` y contract test del documento.

## Cambios

### Tests de arquitectura

Nuevo proyecto `tests/CleanWebApiTemplate.ArchitectureTests`:

- `DependencyRulesTests` (ArchUnitNET):
  - Domain no depende de Application/Infrastructure/Api/ServiceDefaults.
  - Application no depende de Infrastructure/Api/ServiceDefaults.
  - Infrastructure no depende de Api/ServiceDefaults.
  - Application y Domain no referencian ensamblados `Microsoft.AspNetCore.*`.
  - Application no referencia Infrastructure.
- `StructureTests`:
  - `ITodoRepository` vive en `Application.Todo.Abstractions`.
  - `TodoFilter` vive en `Application.Todo.Queries`.
  - `Result<T>` vive en `Application.Results` y no en Domain.
  - `Infrastructure.Repositories` solo contiene repositorios y sus helpers (se excluyen
    tipos anidados generados por el compilador).

### BannedApiAnalyzers

- `Microsoft.CodeAnalysis.BannedApiAnalyzers` se referencia en los cinco proyectos de `src/`.
- `BannedSymbols.txt` prohíbe las APIs de reflexión que rompen AOT:
  `Activator.CreateInstance(Type)`, `Type.GetType`, `Type.GetMethod/GetProperty/GetMember`,
  `Assembly.Load/GetType`, `MethodInfo.Invoke/MakeGenericMethod`,
  `PropertyInfo.GetValue/SetValue` y la serialización de `System.Text.Json` por `Type`.
- Los bans son de **métodos**, no de tipos: el código generado por
  `Microsoft.AspNetCore.OpenApi` usa `PropertyInfo`/`MethodInfo` como tipos y no debe
  disparar `RS0030`.
- Se verificó que un uso de `typeof(string).GetMethod(...)` produce `RS0030`.

### Configuración

`AppConfigurationHelper.ConfigureSources` ahora:

1. construye una configuración bootstrap solo para localizar OpenBao (dirección/token desde
   entorno o línea de comandos);
2. limpia las fuentes que `CreateSlimBuilder` ya había añadido;
3. reconstruye el orden de precedencia de forma explícita:
   `appsettings.json` < `appsettings.{Environment}.json` < user secrets (solo Development)
   < OpenBao < variables de entorno < línea de comandos;
4. mantiene el binding manual de `AppSettings` y `AppSettingsValidator` (AOT-friendly).

### Usuario actual y routing

- `ICurrentUser` vive en `Application.Abstractions` y expone `Email`, `IsAdmin` y
  `OwnerScope` sin tipos de ASP.NET.
- `HttpContextCurrentUser` es el adaptador de Api: lee el claim de email (mapeado o corto) y
  el rol Admin.
- `TodoRoutes` implementa `IGroupMap` directamente y recibe `ICurrentUser`; ya no hereda de
  `BaseApiRouter`.
- `ICurrentUser` se registra como transient: es un adaptador sin estado sobre
  `IHttpContextAccessor`, y el mapeo de rutas ocurre fuera de una petición.
- `BaseApiRouter` y sus tests se eliminaron; el mapeo de rutas conserva `RouteName = "Todo"`
  explícito.

### OpenAPI

`OpenApiConfiguration` se dividió en:

- `OpenApiConfiguration`: título, descripción, tag, seguridad Bearer y registro del
  transformer;
- `CommonOpenApiSchemas`: `Ulid`, `ProblemDetails` y `KeyValuePair<string, bool>`;
- `TodoOpenApiSchemas`: requests y responses de Todo;
- `OpenApiSchemaHelpers`: `Describe`, `Property`, `ArrayItems`, `Patch`, paging, sort y
  status.

El nuevo `OpenApiContractTests` de componente comprueba:

- título, versión, tag y esquema Bearer;
- que las seis operaciones publicadas están presentes y todas las rutas son `/api/...`;
- formato/patrón de `Ulid`, límites de `CreateTodoRequest`, `$ref` de `id`, formato email,
  enum de `status` y descripciones de `ProblemDetails`.

## Tests

| Proyecto | Tests |
|---|---:|
| `Api.UnitTests` | 136 |
| `Api.ComponentTests` | 75 |
| `Api.FunctionalTests` | 11 |
| `ArchitectureTests` | 9 |
| `OpenApiExamples.UnitTests` | 33 |
| **Total** | **264** |

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish src/CleanWebApiTemplate.Api/... -c Release -r linux-x64 --self-contained` | ✅ AOT, 0 warnings |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 264 tests, 0 failed |
| `dotnet format whitespace --verify-no-changes` | ✅ |
| `dotnet format style --verify-no-changes` | ✅ |
| `dotnet format analyzers --verify-no-changes --exclude-diagnostics IL2026 IL3050` | ✅ |

Nota: `dotnet format analyzers` sin exclusiones sigue reportando IL2026/IL3050 en `Map*`
porque no ejecuta el source generator de request delegates; es el falso positivo ya
documentado en `06-tooling.md`.

## Pendiente

- **Batch 5**: cobertura de caminos críticos, script de cobertura y umbral.
- **Batch 6**: hooks y guardarraíles de pre-push/commit-msg.
- **Batch 7**: template y readmes.
