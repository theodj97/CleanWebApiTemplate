# 00 — Baseline

## Objetivo

Fijar una foto reproducible del repositorio antes de emitir juicios: qué se compila, qué se
publica, qué tests pasan, qué cobertura hay y qué herramientas están disponibles.

## Entorno

| Elemento | Valor |
|---|---|
| Fecha | 2026-10-29 |
| Rama | `features/code-audit` |
| Commit | `fbca5cae986bd08950292910fd6d15ea2c434930` |
| SO | Ubuntu 26.04.1 LTS |
| .NET SDK | 10.0.113 |
| Runtime | Microsoft.NETCore.App 10.0.13 / ASP.NET Core 10.0.13 |
| `global.json` | SDK 10.0.100, `rollForward: latestFeature`, runner MTP |
| Docker | 29.1.3 |
| Aspire CLI | 13.6.4 |
| Husky.Net | 0.9.1 (tool local) |
| ReportGenerator | 5.5.11 (tool global) |
| dotnet-coverage | 18.12.0 (tool global) |
| Node / npm | 24.22.0 / 11.19.1 |
| Git | 2.53.0 |

Herramientas **no instaladas** y por tanto no ejecutadas: `gitleaks`, `semgrep`, `trivy`,
`zap-cli`, `dotnet-stryker`, ArchUnitNET. Se proponen comandos en las fases correspondientes.

## Build

```text
dotnet build CleanWebApiTemplate.slnx
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:18.18
```

**Resultado: correcto.** El build no sustituye a la publicación AOT; se verificó aparte.

## Publicación Native AOT

```text
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained
...
Generating native code
CleanWebApiTemplate.Api -> .../bin/Release/net10.0/linux-x64/publish/
```

**Resultado: correcto, 0 warnings de trimming/AOT.** Es la validación autoritativa de AOT;
`dotnet format analyzers` produce falsos positivos (ver `06-tooling.md`).

Además se ejecutó el binario nativo contra el PostgreSQL/OpenBao locales, en otro puerto y con
la configuración del AppHost:

| Prueba | Resultado |
|---|---|
| Arranque del binario nativo | ✅ |
| `GET /health/api` | 200 |
| `GET /openapi/v1.json` | 200 |
| `POST /api/Todo/` válido | 201 con recurso |
| `POST` con `title: null` | 500 (mismo bug que en JIT) |
| `POST` sin `title` | 500 (mismo bug que en JIT) |

Esto confirma que el camino AOT **funciona en runtime** para JSON, FluentValidation, OpenAPI y
PostgreSQL; los 500 no son un problema de AOT, son los bugs de validación/error handling ya
descritos.

## Tests

| Proyecto | Tests | Resultado | Duración |
|---|---:|---|---:|
| `Api.UnitTests` | 113 | 113 passed | ~0,9 s |
| `Api.ComponentTests` | 54 | 54 passed | ~14,0 s |
| `Api.FunctionalTests` | 11 | 11 passed | ~14,1 s |
| `OpenApiExamples.UnitTests` | 33 | 33 passed | ~0,7 s |
| **Total** | **211** | **211 passed, 0 failed, 0 skipped** | **~16,5 s** |

La ejecución conjunta `dotnet test --solution CleanWebApiTemplate.slnx` también pasa: los
proyectos de component y functional arrancan en paralelo con contenedores separados.

## Cobertura

Generada con `dotnet-coverage` global sobre los tres proyectos principales (unit, component y
functional), fusionada y filtrada para incluir solo los cinco ensamblados de `src/` y excluir
`obj/`, `bin/`, tests y `local/`.

| Ensamblado | Líneas | Ramas | Métodos |
|---|---:|---:|---:|
| `CleanWebApiTemplate.Api` | **66,0%** | **56,9%** | — |
| `CleanWebApiTemplate.Application` | **99,7%** | **100%** | — |
| `CleanWebApiTemplate.Domain` | **100%** | **100%** | — |
| `CleanWebApiTemplate.Infrastructure` | **96,8%** | **88,5%** | — |
| `CleanWebApiTemplate.ServiceDefaults` | **95,7%** | **100%** | — |
| **Total `src/`** | **82,7%** | **79,7%** | **94,8%** |

Puntos débiles:

| Clase | Líneas | Ramas |
|---|---:|---:|
| `Api.Configuration.OpenApiConfiguration` | 1,7% | 2,8% |
| `Api.Common.Abstractions.EndpointRouteBuilderExtensions` | 45,4% | 16,6% |
| `Api.Routes.Todo.TodoRoutes` | 91,3% | 14,2% |
| `Infrastructure.Secrets.OpenBaoClient` | 90,0% | 83,3% |
| `Infrastructure.Repositories.TodoRepository` | 98,1% | 89,0% |

El informe antiguo (`coverage-report/Summary.txt`, 2026-10-26) decía 89,9% de líneas y 72,5%
de ramas, pero incluía `Tests.Shared` y `AppHost`; no es comparable con la cobertura real de
`src/`. La cobertura de `Domain` y `Application` está además inflada por tests de constantes,
records de error y `SectionBase` (ver `04-tests-coverage.md`).

Evidencia: [`evidence/coverage-summary.txt`](evidence/coverage-summary.txt) y
[`evidence/coverage-summary.md`](evidence/coverage-summary.md).

## Formato y analyzers

| Comando | Resultado |
|---|---|
| `dotnet format whitespace --verify-no-changes` | ✅ 0 cambios |
| `dotnet format style --verify-no-changes` | ✅ 0 cambios |
| `dotnet format analyzers --verify-no-changes` | ❌ 18 errores `IL2026`/`IL3050` en `MapGet`/`MapPost`/`MapPut`/`MapDelete` |
| `dotnet format analyzers --verify-no-changes --exclude-diagnostics IL2026 IL3050` | ✅ 0 cambios |
| `dotnet build -p:AnalysisMode=Recommended` | ✅ 0 warnings |
| `dotnet build -p:AnalysisMode=All` | ⚠️ 104 CA1707, 68 CA1515, 48 CA2007, 30 CA2234, 26 CA1062, 12 CA1305, 8 CA1819, 8 CA1307, 4 CA2000, … |

Los `IL2026`/`IL3050` de `dotnet format analyzers` son falsos positivos conocidos: la
herramienta no ejecuta el source generator de request delegates y ve la sobrecarga
`Delegate` de `Map*`, que sí está cubierta por `EnableRequestDelegateGenerator` en build y
publish. No se deben silenciar en el código; se debe usar `dotnet build`/`publish` como gate
AOT y excluir esos diagnósticos solo en la tarea de formato, si se añade.

## Paquetes

| Comprobación | Resultado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` | ✅ Sin vulnerabilidades conocidas |
| `dotnet list package --deprecated` | ✅ Sin paquetes deprecados |
| `dotnet list package --outdated` | ⚠️ Varias actualizaciones disponibles |

Actualizaciones relevantes:

| Paquete | Actual | Última |
|---|---|---|
| `FluentValidation` | 12.1.1 | 12.2.0 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | 10.0.14 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.10 | 10.0.14 |
| `Microsoft.Extensions.Configuration` | 10.0.10 | 10.0.14 |
| `Microsoft.OpenApi` | 2.7.5 | 3.11.0 (major) |
| `Swashbuckle.AspNetCore.SwaggerUI` | 10.2.3 | 10.3.1 |
| `xunit.v3.*` | 3.2.2 | 4.0.2 (major) |
| `Microsoft.AspNetCore.Http` (Domain) | 2.3.11 | 2.3.13 (legacy 2.x) |

## Secretos

Se revisó el historial completo con búsquedas de marcadores de clave privada y asignaciones
literales de `password`/`secret`/`token`:

- Sin bloques `BEGIN ... PRIVATE KEY`.
- Sin contraseñas o tokens literales en el historial.
- `local/tools/devtoken.json` solo contiene perfiles, issuer y audiencia; las claves viven en
  user secrets fuera del repositorio.
- `appsettings*.json` no contienen cadenas de conexión ni secretos.

No se ejecutó `gitleaks` (no instalado). Se recomienda añadirlo como gate de pre-commit y
ejecutar `gitleaks detect --log-opts=--all` una vez.

## Pruebas dinámicas

Se arrancó el AppHost local con `aspire start` y se probaron los endpoints con `curl` y tokens
locales. Resumen (detalle en `01-security.md` y `02-correctness.md`):

| Comprobación | Resultado |
|---|---|
| `/health`, `/alive`, `/health/api`, `/health/postgresDb` sin token | 200 |
| `/openapi/v1.json` | 200 |
| `POST /api/Todo/` válido | 201, **sin cabecera `Location`** |
| `POST` con `title: null` | **500** |
| `POST` sin `title` | **500** |
| `POST` con JSON malformado | **500** |
| `PUT` con `status: 0` | 200 y **persiste 0** |
| Filtro por fragmento de título | 200 `[]` (el código hace igualdad exacta) |
| Filtro sin coincidencias | 200 `[]` (la documentación dice 204) |
| Token sin claim `email` | **500** |
| Token HS512 firmado con la misma clave | **201** (no se fija el algoritmo) |
| Cabeceras de seguridad/HSTS | Ausentes |
| Rate limiting | Ausente (50 peticiones, todas 200/405) |
| `GET` a ruta inexistente / método no permitido | 404/405 con body vacío |
| Body de 31 MB | 413 (límite por defecto de Kestrel) |

## Conclusión del baseline

El repositorio está en un estado **funcional y reproducible**: compila, publica AOT, pasa los
tests y no tiene paquetes vulnerables conocidos. Los problemas no son de “no arranca”, sino de
contrato, validación, robustez de bordes, arquitectura y preparación como template.
