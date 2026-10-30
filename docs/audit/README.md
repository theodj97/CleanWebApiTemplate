# Auditoría del repositorio CleanWebApiTemplate

Fecha: 2026-10-29
Rama auditada: `features/code-audit`
Commit: `fbca5cae986bd08950292910fd6d15ea2c434930`

Esta carpeta contiene la auditoría completa por fases. La auditoría original **no modificó
código, configuración ni manifiestos**. Después se implementaron los **Batches 1, 2, 3, 4a, 4b,
5 y 6** de remediación (ver [`09-batch-1.md`](09-batch-1.md), [`10-batch-2.md`](10-batch-2.md),
[`11-batch-3.md`](11-batch-3.md), [`12-batch-4-structure.md`](12-batch-4-structure.md),
[`13-batch-4b.md`](13-batch-4b.md), [`14-batch-5.md`](14-batch-5.md) y
[`15-batch-6.md`](15-batch-6.md)); el resto de batches siguen pendientes.

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
| [`13-batch-4b.md`](13-batch-4b.md) | Batch 4b: architecture tests, banned APIs, configuración, ICurrentUser y OpenAPI. |
| [`14-batch-5.md`](14-batch-5.md) | Batch 5: cobertura de caminos críticos, routing, validación y umbrales. |
| [`15-batch-6.md`](15-batch-6.md) | Batch 6: hooks (pre-commit, commit-msg, pre-push), gitleaks, analyzers y guardarraíles. |
| [`findings.csv`](findings.csv) | Registro completo de hallazgos, filtrable. |

## Resumen ejecutivo

- **No se ha encontrado una vulnerabilidad crítica explotable** (sin bypass de autenticación,
  IDOR/BOLA ni inyección SQL en las rutas revisadas). La parametrización SQL y el filtro de
  ordenación son correctos.
- **La calidad del baseline es buena**: build 0 warnings, AOT publish correcto, 313 tests
  verdes tras Batches 1–6, sin paquetes vulnerables ni deprecados y sin claves privadas en el
  historial.
- **Los mayores riesgos estaban en los bordes**: errores de entrada que devolvían 500,
  validación de `Status = 0`, contrato de unicidad de títulos, claims ausentes que provocaban
  500, falta de rate limiting/security headers/forwarded headers y ausencia de tests de esos
  caminos. Batch 5 cubrió validación, routing y autorización; los controles de borde de
  seguridad quedan para Batch 8.
- **La cobertura real de `src/` es 98,0% de líneas y 95,7% de ramas tras Batch 5** (82,7%/
  79,7% en la auditoría inicial; 83,8%/81,2% tras Batches 1–3), no el 89,9%/72,5% del informe
  antiguo: ese informe incluía `Tests.Shared` y `AppHost`. Api queda en 98,8% líneas / 95,9%
  ramas, frente al 72,0%/65,3% del baseline.
- **La arquitectura ya respeta la regla de dependencias**: `Application` no referencia
  `Infrastructure`, `Domain` no arrastra ASP.NET Core y `Result<T>` vive en Application sin
  semántica HTTP (Batch 4a). Batch 4b añadió los architecture tests, `BannedApiAnalyzers`,
  `ICurrentUser` y el split de OpenAPI.
- **El template todavía no está listo para `dotnet new`**: `template.json` tiene `name` y
  `description` vacíos, GUIDs fijos de `UserSecretsId`, excluye `.gitignore`/`README.md` e
  incluiría `coverage-report/`, `.agents/` y `.tools/` (artefactos locales).
- **Los hooks ya son un guardarraíl real**: `pre-commit` formatea y escanea secretos con
  gitleaks, `commit-msg` valida la convención y `pre-push` ejecuta build, formato, analyzers,
  todos los tests y la auditoría de paquetes. Batch 6 los dejó cableados; quedan las
  herramientas externas (semgrep/trivy/ZAP/Stryker) como pasos manuales.

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

**Estado tras Batches 1, 2, 3, 4a, 4b, 5 y 6:** corregidos SEC-002, SEC-012, BUG-001, BUG-002,
BUG-006, BUG-007, BUG-012 (Batch 1); SEC-001, SEC-003, SEC-007, SEC-008 (Batch 2); BUG-003,
BUG-004, BUG-005, BUG-009, BUG-010, BUG-011, BUG-014 (Batch 3; BUG-015 queda mitigado);
ARCH-001, ARCH-002, ARCH-003, ARCH-004 y ARCH-010 (Batch 4a); ARCH-005, ARCH-006, ARCH-007
y ARCH-008 (Batch 4b); TEST-001, TEST-002, TEST-003, TEST-004, TEST-005, TEST-006, TEST-008
y TEST-012 (Batch 5); y TOOL-001, TOOL-002, TOOL-003/004, TOOL-005, TOOL-007, TOOL-008/009/
010/011, TOOL-013, TOOL-021, TOOL-023, TOOL-025 y TOOL-026 (Batch 6; TOOL-024 queda como
mejora opcional). También se automatizó en local la comprobación de supply chain de SEC-015
(Info); CI sigue fuera de alcance. El registro se mantiene como foto de la auditoría; el
detalle está en `09`–`15`.

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

# Guardarraíles (hooks)
npm run hooks:install
npm run verify            # = bash scripts/pre-push.sh
npm run coverage          # = bash scripts/coverage.sh
bash scripts/gitleaks-staged.sh

# Paquetes
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
dotnet list CleanWebApiTemplate.slnx package --deprecated
dotnet list CleanWebApiTemplate.slnx package --outdated
```

La evidencia completa de comandos y salidas está en [`evidence/`](evidence/).
