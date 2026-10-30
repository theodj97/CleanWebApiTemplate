# Auditoría del repositorio CleanWebApiTemplate

Fecha: 2026-10-29
Rama auditada: `features/code-audit`
Commit: `fbca5cae986bd08950292910fd6d15ea2c434930`

Esta carpeta contiene la auditoría completa por fases. La auditoría original **no modificó
código, configuración ni manifiestos**. Después se implementaron los **Batches 1, 2, 3 y 4a**
de remediación (ver [`09-batch-1.md`](09-batch-1.md), [`10-batch-2.md`](10-batch-2.md),
[`11-batch-3.md`](11-batch-3.md) y [`12-batch-4-structure.md`](12-batch-4-structure.md)); el
resto de batches siguen pendientes.

## Índice

| Documento | Contenido |
|---|---|
| [`00-baseline.md`](00-baseline.md) | Baseline reproducible: build, AOT, tests, cobertura, formato, paquetes y entorno. |
| [`01-security.md`](01-security.md) | Threat model STRIDE, revisión de autenticación/autorización, secretos, entrada, transporte, supply chain y pruebas dinámicas. |
| [`02-correctness.md`](02-correctness.md) | Bugs funcionales, integridad de datos, contrato HTTP/OpenAPI y casos límite. |
| [`03-architecture.md`](03-architecture.md) | Reglas de capas, Clean Architecture, patrones, AOT y fitness functions. |
| [`04-tests-coverage.md`](04-tests-coverage.md) | Pirámide de tests, cobertura real de `src/`, huecos, mutación y flakiness. |
| [`05-quality.md`](05-quality.md) | Código muerto, duplicación, naming, configuración, analyzers y deuda de mantenimiento. |
| [`06-tooling.md`](06-tooling.md) | Husky, `dotnet format`, analyzers, scripts, `Directory.Build.props`, paquetes y DX. |
| [`07-template-docs.md`](07-template-docs.md) | `template.json`, empaquetado, GUIDs, post-actions y matriz README vs realidad. |
| [`08-roadmap.md`](08-roadmap.md) | Priorización P0/P1/P2/P3, batches de remediación y guardarraíles. |
| [`09-batch-1.md`](09-batch-1.md) | Remediación del Batch 1: errores de entrada, 201/204/404/405 y traceId. |
| [`10-batch-2.md`](10-batch-2.md) | Remediación del Batch 2: modos de autenticación, HS256, claims y OIDC. |
| [`11-batch-3.md`](11-batch-3.md) | Remediación del Batch 3: unicidad por owner, índice único, contains, trim y fechas. |
| [`12-batch-4-structure.md`](12-batch-4-structure.md) | Batch 4a: estructura repository-centric, Domain limpio y Result sin HTTP. |
| [`findings.csv`](findings.csv) | Registro completo de hallazgos, filtrable. |

## Resumen ejecutivo

- **No se ha encontrado una vulnerabilidad crítica explotable** (sin bypass de autenticación,
  IDOR/BOLA ni inyección SQL en las rutas revisadas). La parametrización SQL y el filtro de
  ordenación son correctos.
- **La calidad del baseline es buena**: build 0 warnings, AOT publish correcto, 211 tests
  verdes en la auditoría (251 tras Batches 1, 2, 3 y 4a), sin paquetes vulnerables ni
  deprecados y sin claves privadas en el historial.
- **Los mayores riesgos están en los bordes**: errores de entrada que devuelven 500, validación
  de `Status = 0`, contrato de unicidad de títulos, claims ausentes que provocan 500, falta de
  rate limiting/security headers/forwarded headers y ausencia de tests de esos caminos.
- **La cobertura real de `src/` es 82,7% de líneas y 79,7% de ramas** en la auditoría
  (83,8%/81,2% tras Batches 1–3; pendiente de regenerar tras 4a), no el 89,9%/72,5% del
  informe antiguo: ese informe incluía `Tests.Shared` y `AppHost`. La zona más débil es `Api`
  (72,0% líneas / 65,3% ramas), sobre todo `OpenApiConfiguration` (1,7%/2,8%) y `TodoRoutes`
  (91,3% líneas / 14,2% ramas).
- **La arquitectura ya respeta la regla de dependencias**: `Application` no referencia
  `Infrastructure`, `Domain` no arrastra ASP.NET Core y `Result<T>` vive en Application sin
  semántica HTTP (Batch 4a). Quedan pendientes los architecture tests y `BannedApiAnalyzers`.
- **El template todavía no está listo para `dotnet new`**: `template.json` tiene `name` y
  `description` vacíos, GUIDs fijos de `UserSecretsId`, excluye `.gitignore`/`README.md` e
  incluiría `coverage-report/` y `.agents/` (≈8 MB de artefactos locales).
- **Husky está bien instalado pero infrautilizado**: solo `pre-commit` y solo `.cs`; no hay
  `pre-push`, `commit-msg`, build, tests, analyzers ni auditoría de paquetes.

### Hallazgos por severidad

| Severidad | Número |
|---|---:|
| P0 (crítico) | 0 |
| P1 (alto) | 23 |
| P2 (medio) | 41 |
| P3 (bajo) | 46 |
| Info | 6 |
| **Total** | **116** |

La severidad es relativa a “template listo para producción”: un P1 de tooling/template puede
no ser un riesgo de runtime, pero sí un bloqueante para publicar el template.

**Estado tras Batches 1, 2, 3 y 4a:** corregidos SEC-002, SEC-012, BUG-001, BUG-002, BUG-006,
BUG-007, BUG-012 (Batch 1); SEC-001, SEC-003, SEC-007, SEC-008 (Batch 2); BUG-003, BUG-004,
BUG-005, BUG-009, BUG-010, BUG-011, BUG-014 (Batch 3; BUG-015 queda mitigado); y ARCH-001,
ARCH-002, ARCH-003, ARCH-004 y ARCH-010 (Batch 4a). El registro se mantiene como foto de la
auditoría; el detalle está en `09`–`12`.

## Alcance

Incluido:

- `src/`, `tests/`, `local/` (AppHost, inicializadores, sidecar OpenAPI y `devtoken`).
- Configuración: `Directory.Build.props`, `Directory.Packages.props`, `global.json`,
  `appsettings*.json`, `.editorconfig`, `.gitattributes`, `.gitignore`, `.husky`,
  `.config/dotnet-tools.json`, `package.json`, `aspire.config.json`, `.template.config`.
- Documentación del repositorio: `README.md`, `AGENTS.md`, `tests/README.md`,
  `local/local-development.md`, `local/openapi/README.md`.

Excluido por decisión del usuario:

- `.agents/skills/**` y `skills-lock.json` (no se auditan las skills).
- CI/CD (no se crea ni modifica nada de GitHub Actions ni equivalentes).
- Cambios de código, configuración o manifiestos: esta fase es solo auditoría.

## Método

1. Baseline reproducible con `dotnet build`, `dotnet publish` AOT, tests, cobertura y formato.
2. Revisión manual por dimensión con evidencia `archivo:línea`.
3. Pruebas dinámicas contra el AppHost local (`aspire start`) para confirmar los hallazgos
   de contrato y seguridad.
4. Auditoría de paquetes e historial de secretos con las herramientas disponibles.
5. Registro de hallazgos con severidad, confianza, impacto y esfuerzo.
6. Roadmap de remediación separado del orden de auditoría.

## Limitaciones

- **No se han instalado herramientas nuevas** (decisión del usuario). Por tanto no se
  ejecutaron ZAP, Stryker, gitleaks, semgrep, trivy ni ArchUnitNET. Se dejan comandos exactos
  y se marcan como “propuesto, no ejecutado”.
- La cobertura se generó con `dotnet-coverage` global y ReportGenerator global, no con el
  paquete `Microsoft.Testing.Extensions.CodeCoverage` (declarado pero no referenciado).
- La prueba de carga se limitó a comprobaciones puntuales de rate limiting y tamaño de body;
  no se hizo un benchmark formal.
- El smoke test de `dotnet new` se intentó en un `HOME`/`DOTNET_CLI_HOME` temporal, sin tocar
  el entorno global. Falló antes de generar nada con `[Error][MV002] Missing 'name'`; el
  procedimiento completo queda documentado en `07-template-docs.md`.

## Cómo reproducir

```bash
# Baseline
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx

# Formato
dotnet format whitespace CleanWebApiTemplate.slnx --verify-no-changes
dotnet format style CleanWebApiTemplate.slnx --verify-no-changes
dotnet format analyzers CleanWebApiTemplate.slnx --verify-no-changes \
  --exclude-diagnostics IL2026 IL3050

# Paquetes
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
dotnet list CleanWebApiTemplate.slnx package --deprecated
dotnet list CleanWebApiTemplate.slnx package --outdated
```

La evidencia completa de comandos y salidas está en [`evidence/`](evidence/).
