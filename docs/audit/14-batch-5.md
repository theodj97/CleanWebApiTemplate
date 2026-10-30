# 14 — Batch 5: cobertura de los caminos críticos

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Cierre de los huecos de cobertura acordados en el Batch 5:

- tests de los bugs corregidos y de la matriz de autorización (TEST-005);
- tests de routing y políticas (TEST-003/004);
- test de colección vacía (TEST-006, ya cubierto en Batch 1);
- script de cobertura reproducible y umbrales (TEST-012);
- contrato OpenAPI (TEST-002/008, ya cubierto en Batch 4b y ampliado aquí).

## Cambios

### Routing

- `EndpointRouteBuilderExtensions` conserva solo la superficie usada:
  - `CreateBaseRouteGroupBuilder(routeName)`;
  - `CreateAuthorizedRouteGroupBuilder(routeName, authPolicy)`.
- Se eliminó `MapAuthorizedGroup`, que no se usaba y duplicaba la variante autorizada.
- Se eliminaron los parámetros `addOpenApiMetadata` y `openApiParameters`, sin uso actual.
- Los grupos siguen aportando prefijo `/api/{routeName}`, tag, ProblemDetails 400/500 y,
  en la variante autorizada, 401/403 más la política de autorización.

### Tests añadidos

- `OpenApiSchemaTests` (unitarios): schemas sin componentes, schemas ausentes, helpers con
  valores nulos, paging, sort, status, formato/longitud/ejemplo y `Patch`.
- `EndpointRouteBuilderExtensionsTests` (unitarios): prefijo, tag, metadata de errores,
  política de autorización y variante sin política.
- `TodoValidationTests` (componente): ids inválidos, paging incompleto o a cero, fechas
  inválidas o invertidas, status desconocido, sort inválido, owner inválido, fragmento
  demasiado largo, título vacío/solo espacios y body `null`.
- `TodoAuthorizationMatrixTests` (componente): 401 anónimo, 403 sin rol y con rol External,
  y 200 con Operator para GET/filter/titles/PUT.
- `OpenApiContractTests`: códigos de respuesta por operación, `required` de
  `CreateTodoRequest`, seguridad global Bearer y descripciones de filtro.

### Cobertura reproducible

- `.config/dotnet-tools.json` fija `dotnet-coverage` 18.12.0 y
  `dotnet-reportgenerator-globaltool` 5.5.11.
- `scripts/coverage.sh`:
  1. restaura herramientas y compila;
  2. recoge cobertura de unit, component y functional;
  3. fusiona los tres informes;
  4. genera `Cobertura`, `TextSummary` y `MarkdownSummaryGithub` filtrados a `src/`;
  5. comprueba los umbrales y devuelve error si alguno falla.
- `npm run coverage` ejecuta el script.
- Los artefactos se generan en `coverage-report/coverage/` (ya ignorado).

## Cobertura final

Generada con `npm run coverage` el 2026-10-30. Evidencia:
[`evidence/coverage-summary-batch5.txt`](evidence/coverage-summary-batch5.txt) y
[`evidence/coverage-summary-batch5.md`](evidence/coverage-summary-batch5.md).

| Ámbito | Líneas | Ramas | Umbral líneas | Umbral ramas |
|---|---:|---:|---:|---:|
| `src/` total | 98,0% | 95,7% | 90% | 80% |
| Api | 98,8% | 95,9% | 85% | 75% |
| Application | 98,6% | 100% | 95% | 90% |
| Domain | 100% | 100% | 100% | 100% |
| Infrastructure | 95,8% | 87,0% | 90% | 85% |
| ServiceDefaults | 95,7% | 100% | 95% | 100% |

## Tests

| Proyecto | Tests |
|---|---:|
| `Api.UnitTests` | 145 |
| `Api.ComponentTests` | 115 |
| `Api.FunctionalTests` | 11 |
| `ArchitectureTests` | 9 |
| `OpenApiExamples.UnitTests` | 33 |
| **Total** | **313** |

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 313 tests, 0 failed |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ AOT, 0 warnings |
| `dotnet format whitespace/style --verify-no-changes` | ✅ |
| `dotnet format analyzers --exclude-diagnostics IL2026 IL3050` | ✅ |
| `npm run coverage` | ✅ umbrales cumplidos |

## Pendiente / fuera de alcance

- **TEST-007**: mutation testing (Stryker), para una tarea manual o CI.
- **TEST-009**: CORS/security headers/rate limiting, en el Batch 8.
- **TEST-010**: renombrar/dividir `Api.UnitTests`, sin impacto funcional.
- **TEST-011**: aislamiento de variables de entorno en `ComponentTestFixture`.
- **TEST-013/014/015**: tests triviales, benchmarks y `Eventually`, en batches posteriores.
