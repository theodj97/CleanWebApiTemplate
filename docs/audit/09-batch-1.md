# 09 — Remediación Batch 1

Fecha: 2026-10-29
Estado: **implementado y verificado**

## Alcance

Batch 1 del roadmap: errores de entrada, contrato 201/204/404/405 y `traceId`.

| ID | Estado | Cambio |
|---|---|---|
| SEC-002 | ✅ | JSON malformado / campo requerido ausente -> 400 ProblemDetails, no 500. |
| BUG-001 | ✅ | `title: null` -> 400 de validación, no `NullReferenceException`. |
| BUG-002 | ✅ | `status: 0` -> 400 y no se persiste. |
| BUG-006 | ✅ | Colecciones vacías -> 204, no 200 `[]`. |
| BUG-007 | ✅ | 201 incluye `Location: /api/Todo/{id}`. |
| BUG-012 | ✅ | 404/405 (y 401/403) devuelven ProblemDetails, no body vacío. |
| SEC-012 | ✅ | 400/404/405/500 incluyen `traceId`; content type `application/problem+json`. |

## Cambios de código

### ProblemDetails centralizado

Nuevo `src/CleanWebApiTemplate.Api/Configuration/ProblemDetailsFactory.cs`:

- `Create(context, statusCode, detail)` construye un `ProblemDetails` con:
  - `status`;
  - `title` estándar (`ReasonPhrases`);
  - `type` `https://httpstatuses.io/{status}`;
  - `traceId` en `Extensions`.
- `WriteAsync` escribe con `AppJsonSerializerContext.Default.ProblemDetails` (AOT-safe) y
  `application/problem+json; charset=utf-8`.

`GlobalExceptionHandler` ahora:

- `BadHttpRequestException` -> 400 (o 413 si el status es 413), con detalle genérico.
- `JsonException` -> 400.
- Cualquier otra excepción -> 500 con `traceId`, sin filtrar el mensaje.
- Los 4xx se registran como `Warning`; los 500 como `Error`.

`ConfigureApplication` añade `UseStatusCodePages` para que 404/405/401/403 tengan el mismo
contrato. No sobrescribe respuestas que ya han empezado a escribirse.

### Colecciones vacías

`Result<T>` ya no usa `value is IEnumerable<T>` (que fallaba para
`Result<IEnumerable<TodoDto>>`). Ahora usa `IEnumerable` no genérico y excluye `string`:

- `Result<IEnumerable<T>>.Success(empty)` -> `IsNoContent = true`;
- `Result<string>.Success("")` -> no es no-content;
- `Result<object>.Success(Array.Empty<object>())` sigue funcionando.

`ApiResultExtensions` sigue devolviendo 204 antes de ejecutar el mapeo de éxito.

### 201 con Location

`ApiResultExtensions.ToResponse` acepta una factoría opcional
`Func<TDto, string>? createdLocation`. `TodoRoutes.CreateTodo` pasa
`dto => $"/api/Todo/{dto!.Id}"`.

El `Location` es relativo (`/api/Todo/{id}`), válido según RFC 9110. El overload genérico
`ToResponse<T>` también acepta la factoría; las llamadas existentes no cambian.

### Validadores null-safe

- `TodoValidator.ValidateTitle(string, ...)` retorna si el título es null/empty. `NotNullNotEmpty`
  sigue reportando el fallo.
- `TodoValidator.ValidateTitle(IdAndTitle, ...)` retorna si el título es null, tras reportar el
  fallo.
- `TodoValidator.ValidateDescription` retorna si es null/empty.
- `BaseAbstractValidator.ValidateUlid` retorna con un fallo si el id es null/empty.
- `FilteredTodoInputValidator` comprueba `title is not null` antes de `Length`.
- `UpdateTodoInputValidator` valida `Status` siempre que no sea null; `0` ya no se acepta.

## Tests añadidos

### Unitarios (121 en total)

- `ResultTests.Success_WithEmptyGenericCollection_ReturnsNoContent`
- `ResultTests.Success_WithEmptyString_IsNotNoContent`
- `CreateTodoInputValidatorTests.Validate_WithNullTitle_FailsWithoutThrowing`
- `UpdateTodoInputValidatorTests.Validate_WithUnregisteredStatus_Fails` (theory con 0 y 99)
- `GlobalExceptionHandlerTests.TryHandleAsync_WithBadHttpRequestException_WritesBadRequestProblemDetails`
- `GlobalExceptionHandlerTests.TryHandleAsync_WithJsonException_WritesBadRequestProblemDetails`
- `GlobalExceptionHandlerTests.TryHandleAsync_WithException_WritesInternalServerErrorProblemDetails`
  (actualizado: `Internal Server Error`, `application/problem+json`, `traceId`)
- `ApiResultExtensionsTests.ToResponse_WithCreatedResultAndLocation_ReturnsLocationHeader`
- `ApiResultExtensionsTests.ToResponse_WithEmptyCollection_ReturnsNoContent`

### Componente (62 en total)

- `ApiContractTests.Get_UnknownRoute_ReturnsProblemDetails`
- `ApiContractTests.Get_UnsupportedMethod_ReturnsProblemDetails`
- `ApiContractTests.Create_WithMalformedJson_ReturnsBadRequest`
- `ApiContractTests.Create_WithMissingTitle_ReturnsBadRequest`
- `ApiContractTests.Create_WithNullTitle_ReturnsBadRequest`
- `ApiContractTests.Update_WithStatusZero_ReturnsBadRequestAndKeepsTheStoredStatus`
- `ApiContractTests.Filter_WithNoMatches_ReturnsNoContent`
- `ApiContractTests.Titles_WithNoRows_ReturnsNoContent`
- `TodoEndpointsTests.Create_WithValidRequest_PersistsTodoAndCreatedBy` (ahora comprueba
  `Location`)
- `TodoFilteredEndpointsTests.Filter_WithDateRangeOutsideTheTodo_ReturnsNoContent` (204)

### Functional (11 en total)

Sin cambios; siguen pasando.

### OpenAPI examples (33 en total)

Sin cambios; siguen pasando.

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ 0 warnings AOT |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 227 tests, 0 failed |
| `dotnet format whitespace --verify-no-changes` | ✅ |
| `dotnet format style --verify-no-changes` | ✅ |
| `dotnet format analyzers --exclude-diagnostics IL2026 IL3050` | ✅ |
| Smoke test del binario AOT | ✅ health/OpenAPI 200; create 201; null/missing title 400 |
| Cobertura `src/` tras Batch 1 | 83,0% líneas / 80,0% ramas (Api 68,6%) |

La cobertura se regeneró con los mismos comandos que el baseline; el detalle está en
`evidence/coverage-summary-batch1.txt` y `.md`.

### Pruebas dinámicas

| Petición | Antes | Ahora |
|---|---|---|
| `POST {"title":null}` | 500 | **400** Validation Errors, `traceId` |
| `POST {}` | 500 | **400** Bad Request, `traceId` |
| `POST {` | 500 | **400** Bad Request, `traceId` |
| `PUT {"status":0}` | 200, persiste 0 | **400**, persiste 1 |
| `POST /filter` sin coincidencias | 200 `[]` | **204** |
| `POST /titles` sin filas | 200 `[]` | **204** |
| `POST` válido | 201 sin Location | **201 con `Location: /api/Todo/{id}`** |
| `GET /nope` | 404 body vacío | **404 ProblemDetails** |
| `GET /api/Todo/` | 405 body vacío | **405 ProblemDetails** |
| Excepción 500 | 500 JSON genérico sin traceId | **500 ProblemDetails con traceId** |

## Pendiente (fuera de Batch 1)

- **Batch 2**: corregido en [`10-batch-2.md`](10-batch-2.md) (email -> 403, HS256 fijado,
  longitud de `API_KEY`, modos OIDC/simétrico).
- **Batch 3**: unicidad de título global vs per-owner, índice único, filtro contains, trim,
  cultura de fechas, concurrencia.
- **Batch 5**: cobertura de los nuevos caminos (aunque los tests ya cubren los fixes).
- **Batch 6**: hooks/analyzers/cobertura.
- **Batch 7**: template/docs/readmes.

## Archivos modificados

```text
src/CleanWebApiTemplate.Api/Configuration/ProblemDetailsFactory.cs          (nuevo)
src/CleanWebApiTemplate.Api/Configuration/GlobalExceptionHandler.cs
src/CleanWebApiTemplate.Api/ConfigureApplication.cs
src/CleanWebApiTemplate.Api/Extensions/ApiResultExtensions.cs
src/CleanWebApiTemplate.Api/Routes/Todo/TodoRoutes.cs
src/CleanWebApiTemplate.Domain/ResultModel/Result.cs
src/CleanWebApiTemplate.Application/Abstractions/BaseAbstractValidator.cs
src/CleanWebApiTemplate.Application/Todo/TodoValidator.cs
src/CleanWebApiTemplate.Application/Todo/Update/UpdateTodoInputValidator.cs
src/CleanWebApiTemplate.Application/Todo/Filtered/FilteredTodoInputValidator.cs
tests/CleanWebApiTemplate.Api.UnitTests/Api/Configuration/GlobalExceptionHandlerTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Api/Extensions/ApiResultExtensionsTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Domain/ResultModel/ResultTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Todo/CreateTodoInputValidatorTests.cs
tests/CleanWebApiTemplate.Api.UnitTests/Todo/UpdateTodoInputValidatorTests.cs
tests/CleanWebApiTemplate.Api.ComponentTests/ApiContractTests.cs            (nuevo)
tests/CleanWebApiTemplate.Api.ComponentTests/Todo/TodoEndpointsTests.cs
tests/CleanWebApiTemplate.Api.ComponentTests/Todo/TodoFilteredEndpointsTests.cs
```
