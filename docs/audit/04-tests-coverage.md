# 04 — Tests, cobertura y mutación

## Baseline

| Proyecto | Tests | Resultado |
|---|---:|---|
| `Api.UnitTests` | 113 | ✅ |
| `Api.ComponentTests` | 54 | ✅ |
| `Api.FunctionalTests` | 11 | ✅ |
| `OpenApiExamples.UnitTests` | 33 | ✅ |
| **Total** | **211** | **✅ 0 fallos** |

> **Estado Batch 5:** 313 tests verdes (145 unitarios, 115 de componente, 11 funcionales,
> 9 de arquitectura y 33 de OpenAPI examples). Cobertura de `src/`: **98,0% líneas /
> 95,7% ramas**; Api **98,8% / 95,9%**. Detalle en
> [`14-batch-5.md`](14-batch-5.md) y evidencia en
> [`evidence/coverage-summary-batch5.txt`](evidence/coverage-summary-batch5.txt).

La infraestructura de tests es uno de los puntos fuertes del repositorio: Aspire levanta
contenedores reales, los tests de componente usan `WebApplicationFactory<Program>`, los
funcionales arrancan la API como proceso separado, se bloquean llamadas HTTP salientes y los
tokens se firman localmente. No hay dependencia de un IdP real.

## Cobertura real de `src/`

Generada con `dotnet-coverage` global, fusionando unit + component + functional y filtrando
`obj/`, `bin/`, tests y `local/`.

| Ensamblado | Líneas | Ramas |
|---|---:|---:|
| Api | 66,0% | 56,9% |
| Application | 99,7% | 100% |
| Domain | 100% | 100% |
| Infrastructure | 96,8% | 88,5% |
| ServiceDefaults | 95,7% | 100% |
| **Total** | **82,7%** | **79,7%** |

> **Tras Batches 1, 2 y 3:** `src/` 83,8% líneas / 81,2% ramas; Api 72,0% / 65,3%.
> Evidencia: `evidence/coverage-summary-batch3.txt`.
>
> **Tras Batch 5:** `src/` 98,0% líneas / 95,7% ramas; Api 98,8% / 95,9%. Evidencia:
> `evidence/coverage-summary-batch5.txt`.

### Clases con peor cobertura

> Tabla del baseline. Tras Batch 5, `OpenApiConfiguration`, `OpenApiSchemaHelpers`,
> `CommonOpenApiSchemas` y `TodoOpenApiSchemas` están al 100% de líneas, y `TodoRoutes` al
> 100% de líneas / 81,2% de ramas.

| Clase | Líneas | Ramas | Comentario |
|---|---:|---:|---|
| `Api.Configuration.OpenApiConfiguration` | 1,7% | 2,8% | El documento OpenAPI no se verifica. |
| `Api.Common.Abstractions.EndpointRouteBuilderExtensions` | 45,4% | 16,6% | `MapAuthorizedGroup` muerto; `CreateAuthorizedRouteGroupBuilder` poco cubierto. |
| `Api.Routes.Todo.TodoRoutes` | 91,3% | 14,2% | Las ramas de error/autorización no se ejercitan. |
| `Infrastructure.Secrets.OpenBaoClient` | 90,0% | 83,3% | Falta el camino HTTPS/errores de red. |
| `Infrastructure.Repositories.TodoRepository` | 98,1% | 89,0% | Bien; faltan ramas de filtros/errores. |

### Cobertura engañosa

- `Domain` al 100% incluye tests de constantes, records de error y `SectionBase`; no implica
  que las reglas de negocio estén bien cubiertas.
- `Application` al 99,7% incluye tests unitarios de validadores con sustitutos, pero no cubre
  los bugs de integración (status 0, null title, unicidad, filtro contains).
- `ApiResultExtensions` al 100% no detecta el 201 sin `Location` ni el 204 de colecciones
  vacías, porque los tests usan formas genéricas distintas a las reales.

## Huecos de cobertura funcional

| ID | Hueco | Evidencia |
|---|---|---|
| TEST-001 | Cobertura global 82,7/79,7; Api 66/56,9 | `evidence/coverage-summary.txt` |
| TEST-002 | `OpenApiConfiguration` 1,7/2,8 | Sin test del documento OpenAPI |
| TEST-003 | `EndpointRouteBuilderExtensions` 45,4/16,6 | Sin tests de grupos/políticas |
| TEST-004 | `TodoRoutes` ramas 14,2 | Sin tests de errores de routing |
| TEST-005 | Faltan tests de null/malformed/status0/missing claims/204/Location/partial/duplicado per-owner | Bugs confirmados sin regresión |
| TEST-006 | Test de colección vacía usa `Result<object>` | `ResultTests.cs:38-45` |
| TEST-007 | Sin mutation testing | No instalado |
| TEST-008 | Sin contract tests de OpenAPI | No hay test que lea `/openapi/v1.json` |
| TEST-009 | Sin tests de CORS/headers/rate limiting | No hay tests de borde |
| TEST-010 | `Api.UnitTests` prueba Domain/Application/Infrastructure | Nombre engañoso |
| TEST-011 | `ComponentTestFixture` muta env del proceso | Riesgo de flakiness en paralelo |
| TEST-012 | Sin script/umbral de cobertura; CodeCoverage sin referenciar | `Directory.Packages.props:35` |

## Calidad de los tests

### Fortalezas

- **Aislamiento real**: contenedores efímeros, base de datos reseteada con Respawn, tokens
  firmados por ejecución, OpenBao efímero.
- **Datos deterministas**: `TestData` usa semilla derivada del nombre del test; `TodoBuilder`
  genera defaults válidos.
- **Bloqueo de salidas**: `BlockExternalHttpHandler` y `OutboundCallRecorder` impiden llamadas
  reales.
- **Matriz de autenticación**: 401/403 para tokens inválidos, roles y ownership.
- **Functional tests**: flujo completo create/read/update/delete y “sin llamadas salientes”.

### Debilidades

- **Tests de constantes**: `ConstantsTests`, `TodoTableTests`, `SectionBase` tests suben la
  cobertura sin aportar comportamiento.
- **Aserciones débiles**: varios tests solo comprueban `StatusCode` y no el body/headers; el
  201 no comprueba `Location`.
- **Falsa confianza genérica**: `ResultTests` prueba `Result<object>` y no
  `Result<IEnumerable<T>>`.
- **Falta de tests de error**: no hay tests para JSON malformado, `title: null`, `status: 0`,
  claim `email` ausente, 404/405, CORS, headers, rate limiting.
- **Cobertura de ramas baja en Api**: 56,9% global y 14,2% en `TodoRoutes`.
- **Sin mutation testing**: no se sabe si las aserciones detectan cambios de comportamiento.
- **Test project naming**: `Api.UnitTests` contiene tests de Domain/Application/Infrastructure;
  para un template, conviene renombrar a `CleanWebApiTemplate.UnitTests` o dividir.

## Comandos reproducibles

```bash
# Tests
dotnet test --solution CleanWebApiTemplate.slnx

# Cobertura completa (unit + component + functional) y umbrales
npm run coverage
# o directamente:
bash scripts/coverage.sh
```

El script deja los XML y el informe en `coverage-report/coverage/` y falla si no se cumple
alguno de los umbrales de la sección siguiente. Requiere Docker para los tests de componente
y funcionales.

## Umbrales aplicados

| Ámbito | Línea | Rama | Método |
|---|---:|---:|---:|
| `src/` total | ≥ 90% | ≥ 80% | — |
| `Domain` | 100% | 100% | — |
| `Application` | ≥ 95% | ≥ 90% | — |
| `Infrastructure` | ≥ 90% | ≥ 85% | — |
| `Api` | ≥ 85% | ≥ 75% | — |
| `ServiceDefaults` | ≥ 95% | 100% | — |

`scripts/coverage.sh` aplica estos umbrales sobre el informe filtrado de `src/`; los tests,
`local/`, `obj/`, `bin/` y el código generado quedan fuera. No se exige cobertura de métodos:
ReportGenerator la reserva a la versión de pago.

## Mutation testing propuesto

```bash
# Requiere instalar la herramienta (no se hizo en esta auditoría)
dotnet tool install --global dotnet-stryker
dotnet stryker --project src/CleanWebApiTemplate.Application/CleanWebApiTemplate.Application.csproj \
  --test-project tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj
```

Objetivo inicial: **≥ 70% de mutation score** en Application/Domain; ≥ 60% en Infrastructure.
No ejecutarlo en pre-commit; sí en una tarea manual o en CI.

## Tests de contrato OpenAPI

Implementado en `tests/CleanWebApiTemplate.Api.ComponentTests/OpenApiContractTests.cs`:

1. arranca la app;
2. hace `GET /openapi/v1.json`;
3. verifica las propiedades de `TodoResponse` y el `$ref` de `Ulid`;
4. verifica que `CreateTodoRequest.required` incluye `title`;
5. verifica las descripciones de `title`/`filter` y los límites de longitud;
6. verifica los códigos 200/201/204/400/401/403/404/409/500 por operación.

Esto habría detectado BUG-005, BUG-006, BUG-007 y BUG-008.

## Flakiness

No se observó flakiness en la ejecución conjunta (313 tests tras Batch 5, 17,8 s). Riesgos latentes:

- `ComponentTestFixture` modifica `ASPNETCORE_ENVIRONMENT` y varias variables de entorno del
  proceso durante el arranque; si component y functional arrancan a la vez, el proceso
  funcional podría heredar valores. Hoy pasa, pero es frágil.
- `Eventually.AssertAsync` captura cualquier excepción y reintenta; puede ocultar un bug real
  hasta el timeout.
- Los tests de componente comparten base de datos y se ejecutan en serie
  (`parallelizeTestCollections: false`); correcto, pero limita el paralelismo.
- `TestData.Unique` usa una secuencia global no reseteada; no afecta a la corrección, pero los
  valores no son reproducibles entre ejecuciones.

## Conclusión

La infraestructura de tests es mejor que la media de un template. El problema no es “faltan
tests” en general, sino que **faltan los tests de los bordes que importan**: errores de
entrada, claims ausentes, estados inválidos, contrato OpenAPI y controles de seguridad. La
cobertura de líneas es buena en Application/Domain, pero la de ramas en Api es baja y la
cobertura de Domain está inflada por tests triviales.
