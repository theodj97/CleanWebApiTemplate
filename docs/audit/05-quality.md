# 05 — Calidad y limpieza

## Alcance

Revisión de código muerto, duplicación, naming, configuración, analyzers, paquetes y
consistencia general. No se han modificado archivos.

## Hallazgos

| ID | Severidad | Hallazgo | Evidencia |
|---|---|---|---|
| QUAL-001 | P3 | Código muerto: `MapAuthorizedGroup`, `AuthorizationPolicies`, `AuthenticationPolicies`, `AUTHENTICATION_SCHEME`, `CreatedByMaxLength`, `UpdatedByMaxLength`, `SectionName` | `EndpointRouteBuilderExtensions.cs:10`; `Constants.cs:20-33`; `TodoTable.cs:23-24`; `SectionBase.cs:5` |
| QUAL-002 | P3 | `appsettings.INTEGRATION.json` y `STAGING.json` duplicados; `Trace` por defecto | `appsettings.*.json` |
| QUAL-003 | P3 | `NoWarn` con `EnableGenerateDocumentationFile` (no es un warning) | `Directory.Build.props:15` |
| QUAL-004 | P3 | `package.json`/`package-lock.json` sin dependencias reales | `package.json`; `package-lock.json` |
| QUAL-005 | P3 | Paquetes declarados sin referencia: `Microsoft.AspNetCore.Http*` en Domain, `Microsoft.Testing.Extensions.CodeCoverage`, `Microsoft.Testing.Platform` | `Directory.Packages.props:9-10,34-35` |
| QUAL-006 | P3 | `OpenApiConfiguration` hace `Patch` silencioso y describe `updatedAt` inexistente | `OpenApiConfiguration.cs:73-76,289-295` |
| QUAL-007 | P3 | `TodoDto` nullable + null-forgiving en `TodoResponse` | `TodoDto.cs`; `TodoResponse.cs:29-41` |
| QUAL-008 | P3 | Typo “validanting” y mensajes inconsistentes | `BaseAbstractValidator.cs:130`; `TodoValidator.cs` |
| QUAL-009 | P3 | `TodoTable` usa `byte` para max lengths y duplica constraints | `TodoTable.cs:21-24` |
| QUAL-010 | P3 | `HashSet` de sort duplicado en cada validación | `FilteredTodoInputValidator.cs:49-61`; `GetTodoTitlesInputValidator.cs:19-31` |
| QUAL-011 | P3 | `AddEndpointsApiExplorer` innecesario con `AddOpenApi` | `ConfigureServices.cs:55` |
| QUAL-012 | P3 | Sin `NpgsqlDataSource` ni transacciones/concurrencia optimista | `NpgsqlConnectionFactory.cs`; `TodoRepository.cs` |
| QUAL-013 | P3 | `OpenBaoClient` sin retry/timeout por política | `OpenBaoClient.cs` |
| QUAL-014 | P3 | `ServiceDefaults` sin service discovery/resiliencia | `ServiceDefaults/Extensions.cs` |
| QUAL-015 | P3 | CA1848: `LoggerMessage` no usado | `GlobalExceptionHandler.cs:23-24` |
| QUAL-016 | P3 | CA1052/CA1716/CA1715/CA1310/CA2234 en `src/` | `build-analysis-all.log` |

## Código muerto y duplicación

### Muerto

- `EndpointRouteBuilderExtensions.MapAuthorizedGroup`: no se llama; `CreateAuthorizedRouteGroupBuilder`
  hace lo mismo.
- `Constants.AuthorizationPolicies` y `AuthenticationPolicies`: solo los usan tests.
- `Constants.AUTHENTICATION_SCHEME`: solo tests.
- `TodoTable.CreatedByMaxLength`/`UpdatedByMaxLength`: solo tests; no se aplican en validación.
- `SectionBase.SectionName`/`AppSettings.SECTION_EXTENSION`: `SectionName` no se usa; la
  jerarquía `SectionBase` solo la hereda `ConnectionStringsSection` y no aporta nada.
- `BaseApiRouter.UserName`/`Role`: no se usan en producción; solo tests.
- `Microsoft.AspNetCore.Http*` en Domain: sin usos.
- `Microsoft.Testing.Extensions.CodeCoverage` y `Microsoft.Testing.Platform` en
  `Directory.Packages.props`: sin referencias en csproj.

### Duplicado

- `appsettings.INTEGRATION.json` y `appsettings.STAGING.json` son casi idénticos.
- `FilteredTodoInputValidator` y `GetTodoTitlesInputValidator` duplican la lista de sort keys.
- `MapAuthorizedGroup` y `CreateAuthorizedRouteGroupBuilder` duplican la lógica de políticas.
- `TodoTable` max lengths y `OpenApiConfiguration` max lengths describen las mismas reglas en
  dos sitios; pueden divergir.
- `TodoDto` y `TodoResponse` repiten el contrato; `TodoTitleResponse` repite id/title.

## Naming y consistencia

- `CleanWebApiTemplate.Api.UnitTests` contiene tests de Domain/Application/Infrastructure; el
  nombre sugiere que solo prueba Api.
- Namespace `CleanWebApiTemplate.Api.Routes.Todo.Get` usa una palabra reservada (`Get`);
  CA1716 lo marca. Renombrar a `GetById`/`Read`.
- `IBaseResponse<DtoType, ResponseType>` no sigue la convención `T` de parámetros genéricos
  (CA1715).
- Mensajes de validación: `"Property 'Title'..."` vs `"Property 'title'..."`, typo
  “validanting”, y `nameof(title)` en minúscula.
- `TodoTable.TitleMaxLength` es `byte`; `DescriptionMaxLength` es `int`; inconsistente.
- `SectionBase`/`SectionName` no se usan; complejidad innecesaria.

## Configuración

### `Directory.Build.props`

Solo activa `EnforceCodeStyleInBuild` y añade un `NoWarn` ineficaz. No centraliza
`TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` ni analyzers. Los
proyectos de test no tienen `TreatWarningsAsErrors`, así que un warning futuro en tests no
rompe el build.

### `Directory.Packages.props`

Centraliza versiones (bien), pero:
- hay paquetes sin referencia;
- no hay lock files;
- no hay configuración explícita de `NuGetAudit`;
- `Microsoft.AspNetCore.Http` 2.3.11 es legacy/EOL en Domain;
- `Microsoft.OpenApi` 2.7.5 tiene una major 3.x disponible;
- `xunit.v3.*` 3.2.2 tiene una major 4.x disponible.

### `.editorconfig`

Cubre formato C# básico y algunas reglas IDE, pero faltan:
- `charset = utf-8`;
- reglas para JSON/YAML/Markdown/SQL;
- convenciones de naming (`dotnet_naming_rule.*`);
- severidades CA (CA1305, CA1310, CA1062, CA1819, CA2007, etc.);
- `dotnet_diagnostic.IDE0005.severity` está en warning, pero `GenerateDocumentationFile` solo
  se activa en Api; en librerías IDE0005 se aplica con `dotnet format`.

### `.gitattributes`

Solo fuerza LF en `*.cs`. `.editorconfig` dice `end_of_line = lf` para todo, pero en Windows
los `.json`, `.md`, `.yml`, `.sql`, `.sh`, `.props`, `.csproj` y `.slnx` podrían quedar en CRLF.
Añadir patrones explícitos.

## Analyzers

`dotnet build` normal: 0 warnings. `AnalysisMode=Recommended`: 0 warnings.
`AnalysisMode=All`: 104 CA1707, 68 CA1515, 48 CA2007, 30 CA2234, 26 CA1062, 12 CA1305,
8 CA1819, 8 CA1307, 4 CA2000, etc.

Interpretación:

- **CA1707** (underscores) y **CA1515** (internal types) son ruido en tests/Api; se pueden
  suprimir por carpeta.
- **CA2007** (`ConfigureAwait`) no es necesario en ASP.NET Core; suprimir en `src/` o no usar
  `All`.
- **CA1062** (null checks) aplica a métodos públicos; muchos pueden ser `internal`.
- **CA1305/CA1310** son reales: cultura en fechas y `EndsWith` sin `StringComparison`.
- **CA1819** (arrays) es intencional en DTOs de request; suprimir en esos tipos.
- **CA2234** es real: `Results.Created(string.Empty, ...)` debería usar `Uri`.
- **CA1848** es una mejora de rendimiento de logging, no un bug.

Recomendación: `AnalysisMode=Recommended` + un conjunto curado de reglas en `.editorconfig`, no
`All` sin supresiones.

## Paquetes

| Paquete | Estado | Acción |
|---|---|---|
| `Microsoft.AspNetCore.Http` / `.Abstractions` 2.3.11 en Domain | Legacy, sin uso | Eliminar |
| `Microsoft.Testing.Extensions.CodeCoverage` 18.9.0 | Declarado, sin referencia | Referenciar en tests o eliminar |
| `Microsoft.Testing.Platform` 2.3.3 | Declarado, sin referencia | Eliminar si MTP ya lo trae |
| `Microsoft.OpenApi` 2.7.5 | Major 3.11.0 disponible | Evaluar migración |
| `xunit.v3.*` 3.2.2 | Major 4.0.2 disponible | Evaluar migración |
| `FluentValidation` 12.1.1 | Minor 12.2.0 | Actualizar |
| `Microsoft.AspNetCore.*` 10.0.10 | Patch 10.0.14 | Actualizar |
| `Swashbuckle.AspNetCore.SwaggerUI` 10.2.3 | Patch 10.3.1 | Actualizar |

No hay vulnerabilidades ni deprecados en la fecha de la auditoría.

## Configuración de ejemplo en Production/Staging

`appsettings.PRODUCTION.json` y `appsettings.STAGING.json` contienen
`https://app.example.com` y `https://login.example.com`. No son secretos, pero son valores de
ejemplo que un usuario podría desplegar sin cambiarlos. Para un template, es mejor usar
placeholders explícitos o dejar los arrays vacíos y documentar que deben configurarse.

## Logging

`appsettings.json` arranca en `Trace`/`Debug`; `INTEGRATION` y `STAGING` también. En producción
se sobreescribe a `Error`, pero un entorno mal configurado puede acabar en `Trace`. Para un
template, el default debería ser `Information` y los overrides por entorno explícitos.

## Conclusión

El código es legible y coherente, pero arrastra deuda de “template en construcción”: código
muerto, paquetes sin usar, configuración duplicada, defaults ruidosos y analyzers sin curaduría.
Nada de esto bloquea el runtime, pero sí afecta a la calidad percibida y al mantenimiento.
