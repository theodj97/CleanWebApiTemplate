# 11 — Remediación Batch 3

Fecha: 2026-10-30
Estado: **implementado y verificado**

> **Actualización posterior (migración de responsabilidades):** el duplicado de título ya no se
> comunica con excepciones ni con 400. La unicidad la comprueba el servicio de aplicación, que
> devuelve `ConflictError` (409), y el repositorio traduce el `23505` concurrente a un
> `Result` fallido con `ConflictError`. `DuplicateTitleException` no existe en el código.

## Alcance

Batch 3 del roadmap: integridad de datos y reglas de negocio.

| ID | Estado | Cambio |
|---|---|---|
| BUG-003 | ✅ | Unicidad de título **por owner**, no global. |
| BUG-004 | ✅ | Índice único `(CreatedBy, Title)` y manejo del conflicto. |
| BUG-005 | ✅ | Filtro de título `contains` con `ILIKE` y escape de comodines. |
| BUG-009 | ✅ | Fechas con `CultureInfo.InvariantCulture` y `RoundtripKind`. |
| BUG-010 | ✅ | Título recortado antes de validar, guardar y comparar. |
| BUG-011 | ✅ | Off-by-one del fragmento de título corregido. |
| BUG-014 | ✅ | `UpdateAsync` devuelve `Result<bool>`; conflicto concurrente -> 409. |
| BUG-015 | ⚠️ Mitigado | Fechas invariantes; `Ulid.Parse`/`DateTime.Parse` quedan detrás de la validación. |

## Decisiones

### Unicidad por owner

La documentación y los DTOs ya decían “unique per owner”; el código hacía unicidad global.
Se ha alineado el código con el contrato:

- `TitleExistsAsync(title, createdBy, excludeId?, ct)`.
- En creación, el owner es `CreateTodoInput.CreatedBy`.
- En actualización, el owner se lee de la fila almacenada (`todoDb.CreatedBy`), así un admin
  que actualiza el TODO de otro usuario comprueba contra los títulos de ese usuario.
- Dos owners distintos pueden usar el mismo título.

### Índice único

`local/database/0001__initial_schema.sql` añade:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Todo_CreatedBy_Title"
    ON "Todo" ("CreatedBy", "Title");
```

El servicio comprueba la unicidad antes de escribir y devuelve **409 Conflict**. Si dos peticiones
pasan el prechequeo a la vez, PostgreSQL rechaza la segunda con `23505`; el repositorio lo traduce
a un `Result` fallido con `ConflictError` (sin lanzar excepciones de negocio) y el servicio
devuelve 409. Se probó con dos `POST` concurrentes: **201 + 409**, sin duplicado.

### Filtro contains

`SearchAsync` ya no usa `Title IN (...)`. Ahora genera:

```sql
"Title" ILIKE @p ESCAPE '\'
```

con `%...%` como valor y escape de `\`, `%` y `_` en la entrada. Es case-insensitive y no
permite inyección de comodines.

### Trim y fechas

- `CreateTodoInput.ToEntity` guarda `Title.Trim()`.
- `UpdateTodoInput.PatchFields` recorta antes de comparar y guardar.
- `NotNullNotEmpty` rechaza whitespace, no solo cadena vacía.
- `ValidateDateTime`, `ValidateStartDateAndEndDate` y `FilteredTodoInput.ToFilter` usan
  `CultureInfo.InvariantCulture` y `DateTimeStyles.RoundtripKind`.
- Los tests formatean fechas con cultura invariante.

### Update concurrente

`UpdateTodoService` ahora:

- comprueba la unicidad contra el owner de la fila y devuelve 409;
- comprueba el `bool` de `UpdateAsync`; si la fila desapareció, devuelve 404;
- propaga el `Result` fallido de una escritura concurrente y devuelve 409.

No se ha añadido todavía control de concurrencia optimista por versión; queda como deuda P3
(Batch 9), porque el caso de uso actual no pierde integridad: el último update gana.

## Tests añadidos

### Unitarios (137 en total, +3)

- `TodoValidatorTests` y los tests de los validadores de Create/Update: reglas de formato, sin
  consultas a la base de datos.
- `CreateTodoServiceTests` (tras la actualización):
  - duplicado del owner -> `ConflictError`, sin insertar;
  - mismo título para otro owner -> inserta y consulta con el owner correcto;
  - fallo del repositorio -> se propaga sin lanzar;
  - título con espacios -> se consulta y se devuelve recortado.
- `UpdateTodoServiceTests` (tras la actualización):
  - duplicado del owner -> `ConflictError`, sin actualizar;
  - fallo del repositorio -> se propaga sin lanzar;
  - fila desaparecida -> `NotFoundError`.
- `TodoTableTests` verifica el nombre del índice único.

### Componente (71 en total, +7)

- `Create_WithDuplicateTitleForOwner_ReturnsConflict`.
- `Create_WithSameTitleForDifferentOwners_ReturnsCreated`.
- `Create_WithPaddedTitle_StoresTrimmedTitle`.
- `Create_WithWhitespaceTitle_ReturnsBadRequest`.
- `Update_WithTitleUsedByAnotherOwner_ReturnsOk`.
- `Insert_WithDuplicateTitleForOwner_ReturnsConflictResult` (tras la actualización).
- `Filter_WithTitleFragment_ReturnsMatchingTodos`.
- `Filter_WithLikeWildcard_TreatsItLiterally`.
- `Filter_WithWhitespaceTitle_ReturnsBadRequest`.

### Functional / OpenAPI examples

Sin cambios; siguen pasando (11 + 33).

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ 0 warnings AOT |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ **252 tests**, 0 failed |
| `dotnet format whitespace/style` | ✅ |
| `dotnet format analyzers` (excluyendo IL2026/IL3050) | ✅ |
| Cobertura `src/` tras Batch 3 | 83,8% líneas / 81,2% ramas |
| Api | 72,0% líneas / 65,3% ramas |
| Smoke test AOT | ✅ 201 válido, 400 null/missing, 403 sin email, 401 HS512 |

### Pruebas dinámicas

| Prueba | Resultado |
|---|---|
| Mismo título, owners distintos | 201 + 201 |
| Mismo título, mismo owner | 409 “A TODO with this title already exists for this owner.” |
| Dos `POST` concurrentes con el mismo título/owner | 201 + 409 (índice único) |
| Título con espacios | se guarda recortado |
| Filtro por fragmento `fragment` | devuelve los dos TODOs que lo contienen |
| Filtro con `%` literal | solo coincide el título con `%`, no el comodín |
| Filtro con título whitespace | 400 (no devuelve todos los TODOs) |
| Update a un título de otro owner | 200 |
| Update a un título del mismo owner | 409 |

## Pendiente

- **Batch 4**: arquitectura (Application -> Infrastructure, Domain -> ASP.NET, Result).
- **Batch 5/6/7**: cobertura de OpenAPI, mutation/hooks y template/readmes.
- **Batch 9 (P3)**: concurrencia optimista por versión si se quiere evitar “last write wins”.

## Archivos modificados

```text
src/CleanWebApiTemplate.Infrastructure/Constants/TodoTable.cs
src/CleanWebApiTemplate.Infrastructure/Repositories/Interfaces/ITodoRepository.cs
src/CleanWebApiTemplate.Infrastructure/Repositories/TodoRepository.cs
local/database/0001__initial_schema.sql
src/CleanWebApiTemplate.Application/Abstractions/BaseAbstractValidator.cs
src/CleanWebApiTemplate.Application/Todo/TodoValidator.cs
src/CleanWebApiTemplate.Application/Todo/Create/CreateTodoInput.cs
src/CleanWebApiTemplate.Application/Todo/Create/CreateTodoInputValidator.cs
src/CleanWebApiTemplate.Application/Todo/Create/CreateTodoService.cs
src/CleanWebApiTemplate.Application/Todo/Update/UpdateTodoInput.cs
src/CleanWebApiTemplate.Application/Todo/Update/UpdateTodoInputValidator.cs
src/CleanWebApiTemplate.Application/Todo/Update/UpdateTodoService.cs
src/CleanWebApiTemplate.Application/Todo/Filtered/FilteredTodoInput.cs
src/CleanWebApiTemplate.Application/Todo/Filtered/FilteredTodoInputValidator.cs
tests/CleanWebApiTemplate.Api.UnitTests/Application/Todo/TodoValidatorTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Todo/CreateTodoInputValidatorTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Todo/UpdateTodoInputValidatorTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Infrastructure/Constants/TodoTableTests.cs
tests/CleanWebApiTemplate.Api.ComponentTests/Todo/TodoEndpointsTests.cs
tests/CleanWebApiTemplate.Api.ComponentTests/Todo/TodoFilteredEndpointsTests.cs
tests/CleanWebApiTemplate.Tests.Shared/Data/TodoTestRepository.cs
```
