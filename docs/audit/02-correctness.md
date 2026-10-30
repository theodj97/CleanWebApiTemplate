# 02 — Correctitud e integridad de datos

## Alcance

Se revisaron los servicios de aplicación, validadores, repositorio ADO.NET, mapeo DTO/respuesta,
contrato HTTP y OpenAPI. Los hallazgos se confirmaron con pruebas dinámicas cuando fue posible.

> **Estado:** Batch 1 corrigió BUG-001, BUG-002, BUG-006, BUG-007 y BUG-012. Batch 3 corrigió
> BUG-003, BUG-004, BUG-005, BUG-009, BUG-010, BUG-011 y BUG-014; BUG-015 queda mitigado (ver
> [`09-batch-1.md`](09-batch-1.md) y [`11-batch-3.md`](11-batch-3.md)). El resto sigue
> pendiente.

## Hallazgos

| ID | Severidad | Hallazgo | Evidencia | Reproducción |
|---|---|---|---|---|
| BUG-001 | P1 | `title: null` produce `NullReferenceException` y 500 | `TodoValidator.cs:17`; `CreateTodoInputValidator.cs:10-12` | `POST {"title":null}` -> 500 |
| BUG-002 | P1 | `status: 0` se acepta y persiste | `UpdateTodoInputValidator.cs:25-27`; `UpdateTodoInput.cs:29-33` | `PUT {"status":0}` -> 200, GET status 0 |
| BUG-003 | P1 | Unicidad de título global, documentada como per-owner | `TodoRepository.cs:192-208`; `TodoRoutes.cs:96` | Crear mismo título como user y operator -> 400 |
| BUG-004 | P2 | Sin índice único ni transacción para la unicidad | `local/database/0001__initial_schema.sql:9-22`; `CreateTodoService.cs` | Revisión de código; carrera validar->insertar |
| BUG-005 | P2 | Filtro por título hace igualdad exacta, no “contains” | `TodoRepository.cs:118-119`; `FilteredTodoRequest.cs:11` | Filtrar por fragmento -> 200 `[]` |
| BUG-006 | P2 | Colección vacía devuelve 200 `[]`, no 204 | `Result.cs:19-20`; `FilteredTodoService.cs:18` | Filtrar sin coincidencias -> 200 `[]` |
| BUG-007 | P2 | 201 Created sin cabecera `Location` | `ApiResultExtensions.cs:14` | `POST` válido -> 201, sin `Location` |
| BUG-008 | P3 | `TodoEntity.ToDto()` no copia `UpdatedAt`; `TodoResponse` no lo expone | `TodoEntity.cs:17-26`; `TodoResponse.cs`; `OpenApiConfiguration.cs:190-193` | Revisión de código/OpenAPI |
| BUG-009 | P2 | Fechas dependientes de cultura | `BaseAbstractValidator.cs:44-48`; `FilteredTodoInput.cs:28-29` | Revisión; tests usan `ToString` sin cultura |
| BUG-010 | P3 | Título no se recorta aunque la documentación dice “trimmed” | `CreateTodoService.cs:17` | `POST {"title":"  x  "}` -> se guarda con espacios |
| BUG-011 | P3 | Off-by-one en max length del fragmento de título | `FilteredTodoInputValidator.cs:18` | Revisión de código |
| BUG-012 | P3 | 404/405 devuelven body vacío | `ConfigureApplication.cs` (sin `UseStatusCodePages`) | `GET /nope`, `GET /api/Todo/` -> body vacío |
| BUG-013 | P2 | RS256 no funciona de extremo a extremo | `ConfigureServices.cs:177-179`; `AppHost.cs:140-155`; `OpenBao.cs:36-38` | Activar `LocalAuth:Algorithm=RS256` -> falla el seed/arranque |
| BUG-014 | P3 | `UpdateAsync` ignora el `bool` y no hay concurrencia optimista | `UpdateTodoService.cs:25`; `TodoRepository.cs:167-178` | Revisión; pérdida de updates posible |
| BUG-015 | P3 | `Ulid.Parse`/`DateTime.Parse` pueden lanzar si se salta la validación | `FilteredTodoInput.cs:24-29` | Revisión; `TryParse` recomendado |

## Detalle de los bugs principales

### BUG-001 — `title: null` -> 500

`CreateTodoInputValidator` encadena `NotNullNotEmpty` y `ValidateTitle`. FluentValidation
ejecuta ambas reglas (el cascade es `Continue`), y `ValidateTitle` hace `title.Length` sin
comprobar null. El resultado es `NullReferenceException`, capturada por
`GlobalExceptionHandler` y convertida en 500.

**Impacto:** un cliente puede provocar 500 con un body sintácticamente válido; se ensucian los
logs y se rompe el contrato (debería ser 400).

**Fix:** null-check antes de `Length`, `required` real en el DTO o `[Required]`, y test de
regresión. El mismo patrón aplica a `UpdateTodoInputValidator`/`TodoValidator`.

### BUG-002 — `status: 0` se persiste

`UpdateTodoInputValidator` solo valida el estado cuando `Status is not null && Status != 0`.
`PatchFields` aplica cualquier `Status` no nulo, incluido 0. El enum empieza en 1, así que 0
es un valor inválido que queda en la base de datos.

**Impacto:** integridad de datos; filtros por estado y futuras migraciones pueden fallar.

**Fix:** validar siempre el enum cuando `Status is not null`; si 0 significa “no cambiar”, no
aplicarlo en `PatchFields`. Añadir test unitario y de componente.

### BUG-003 — Unicidad global vs per-owner

La documentación de la API, los DTOs y OpenAPI dicen “unique per owner”
(`TodoRoutes.cs:96`, `OpenApiConfiguration.cs:109,121`). El repositorio llama a
`TitleExistsAsync(title, excludeId)` sin `createdBy`, por lo que la unicidad es global. La
prueba dinámica lo confirma: el mismo título creado por `user` y por `operator` devuelve 400.

**Decisión pendiente:** ¿la regla es global o por owner? Si es por owner, hay que:
- añadir `createdBy` a `TitleExistsAsync` y a la query;
- crear un índice único `("CreatedBy", "Title")`;
- manejar la violación de unicidad como 400/409;
- actualizar tests y documentación.

Si es global, hay que corregir la documentación y los comentarios, y añadir el índice único
sobre `Title`.

### BUG-004 — Carrera validar -> insertar

Aunque se decida la regla, el chequeo de unicidad y el `INSERT` son dos operaciones separadas.
Dos peticiones concurrentes con el mismo título pueden pasar ambas la validación. Sin un
índice único, se insertan duplicados. Con índice único, la segunda inserta falla y hay que
traducir la excepción de PostgreSQL a un 409/400.

### BUG-005 — Filtro “contains” que no contiene

`FilteredTodoRequest.Title` y OpenAPI describen “Title fragments to match (contains)”. El
repositorio usa `"Title" IN (@p0, ...)`, es decir, igualdad exacta. La prueba con el fragmento
`audit-shared` devolvió `[]` aunque existían títulos que lo contenían.

**Fix:** `ILIKE '%' || @p || '%'` con escape de `%`/`_`, o `position(lower(@p) in lower("Title")) > 0`.
Mantener el parámetro y añadir test de fragmento.

### BUG-006 — 204 que nunca llega

`Result<T>` marca `IsNoContent` cuando `value is null` o cuando `value is IEnumerable<T>` y
está vacío. En `Result<IEnumerable<TodoDto?>>`, `T` es `IEnumerable<TodoDto?>`, así que la
comprobación busca `IEnumerable<IEnumerable<TodoDto?>>` y nunca coincide. Los endpoints de
listas devuelven 200 `[]` donde OpenAPI y los comentarios dicen 204.

**Fix:** una factoría específica para colecciones (`Result<IEnumerable<T>>.Success(collection)`
que compruebe `collection.Any()`), o cambiar la representación para que `T` sea el elemento.
Añadir test con la forma genérica real.

### BUG-007 — 201 sin Location

`ApiResultExtensions` usa `Results.Created(string.Empty, ...)`. ASP.NET Core no genera una
cabecera `Location` útil; la prueba dinámica no encontró `Location`. Un 201 debería apuntar al
recurso creado (`/api/Todo/{id}`).

### BUG-008 — `UpdatedAt` inconsistente

`TodoEntity.ToDto()` copia `CreatedAt` pero no `UpdatedAt`; `TodoResponse` tampoco expone
`UpdatedAt`. Sin embargo, `OpenApiConfiguration` describe `updatedAt` en el schema de
`TodoResponse` (la llamada a `Property` devuelve null y el `Describe` es un no-op silencioso).
Hay que decidir si el campo se expone o no, y alinear DTO, respuesta y OpenAPI.

### BUG-009 — Fechas y cultura

`BaseAbstractValidator.ValidateDateTime` usa `DateTime.TryParse` sin cultura ni estilos.
`FilteredTodoInput.ToFilter` usa `DateTime.Parse`. Los tests formatean fechas con
`ToString("yyyy-MM-dd HH:mm:ss")` sin `CultureInfo.InvariantCulture`. En un contenedor con otra
cultura, el mismo body puede validar/parsear distinto.

**Fix:** `DateTimeOffset.TryParse(..., CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)`
y usar `DateTimeOffset` en el filtro; tests con cultura invariante.

### BUG-010 — “Trimmed” que no recorta

La documentación dice que el título se recorta; el servicio lo guarda tal cual. La prueba con
`"  audit-padded-title  "` conservó los espacios. Esto afecta también a la unicidad: `"x"` y
`" x "` son títulos distintos.

### BUG-012 — 404/405 vacíos

`AddProblemDetails()` no añade automáticamente ProblemDetails a los status codes generados por
el routing. `GET /nope` y `GET /api/Todo/` devuelven 404/405 con `content-length: 0`. Añadir
`UseStatusCodePages()` o un middleware equivalente para que el contrato sea consistente.

### BUG-013 — RS256 incompleto

`LocalJwt.Create` acepta `RS256` y `TestTokenIssuer` lo soporta, pero:
- `AppHostContract.JwtTrust` para la API no mapea `PublicKeyPem`;
- `ConfigureServices` siempre lee `API_KEY` y crea una `SymmetricSecurityKey`;
- `OpenBao.AddOpenBaoInitializer` exige `localJwt.SigningKey` (HS256) y lanza si es RS256.

Resultado: activar RS256 rompe el arranque. Hay que completar el flujo (clave pública en
configuración, `IssuerSigningKey` asimétrica, seed sin clave simétrica) o eliminar la opción y
documentar HS256-only.

## Contrato HTTP/OpenAPI

| Operación | Documentado | Real | Estado |
|---|---|---|---|
| `POST /api/Todo` | 201 con recurso | 201 con `Location` | ✅ Batch 1 |
| `GET /api/Todo/{id}` | 200 o 204 | 200 o 204 | ✅ |
| `POST /api/Todo/filter` | 200 o 204 si vacío | 204 si vacío | ✅ Batch 1 |
| `POST /api/Todo/titles` | 200 o 204 si vacío | 204 si vacío | ✅ Batch 1 |
| `PUT /api/Todo/{id}` | 200 / 404 / 400 | 200 / 404 / 400; `status=0` -> 400 | ✅ Batch 1 |
| `DELETE /api/Todo/{id}` | 200 bool | 200 bool | ✅ |
| `GET /nope` | ProblemDetails | 404 ProblemDetails | ✅ Batch 1 |
| `GET /api/Todo` | 405 ProblemDetails | 405 ProblemDetails | ✅ Batch 1 |
| `TodoResponse` | incluye `updatedAt` en OpenAPI | no lo incluye | ❌ BUG-008 (Batch 9) |
| `CreateTodoRequest.title` | required, trimmed, unique per owner | required, trimmed, unique per owner | ✅ Batch 3 |
| `FilteredTodoRequest.title` | contains | contains (ILIKE, comodines escapados) | ✅ Batch 3 |

## Decisiones

1. **Unicidad de título:** decidido **por owner** e implementado con índice único (Batch 3).
2. **`UpdatedAt`:** pendiente; decidir si se expone en `TodoResponse` o se elimina del schema
   OpenAPI y de `TodoDto` (Batch 9).
3. **`Status = 0`:** decidido inválido; se valida siempre (Batch 1).
4. **204 en listas:** decidido 204 para colecciones vacías (Batch 1).
5. **RS256 local:** eliminado; OIDC es el camino asimétrico en despliegue (Batch 2).

## Tests de regresión propuestos

- `Create_WithNullTitle_ReturnsBadRequest` (no 500).
- `Create_WithMissingTitle_ReturnsBadRequest` (no 500).
- `Create_WithMalformedJson_ReturnsBadRequest` (no 500).
- `Update_WithStatusZero_ReturnsBadRequest` y no persiste 0.
- `Create_WithSameTitleForDifferentOwners_...` según la decisión.
- `Filter_WithTitleFragment_ReturnsMatchingTodos`.
- `Filter_WithNoMatches_ReturnsNoContent` (o 200 `[]`, según decisión).
- `Create_WithValidRequest_ReturnsCreatedWithLocation`.
- `GetTodoById_WithTokenWithoutEmail_ReturnsForbidden` (no 500).
- `OpenApiDocument_DescribesTheRuntimeContract`.
