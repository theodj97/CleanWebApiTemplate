# Evidencia — comandos y resultados

Fecha: 2026-10-29. Rama `features/code-audit`, commit `fbca5cae986bd08950292910fd6d15ea2c434930`.

## Build

```bash
dotnet build CleanWebApiTemplate.slnx
```

Resultado: `Build succeeded. 0 Warning(s) 0 Error(s)` (18,18 s).

## AOT

```bash
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained
```

Resultado: publicación correcta, `Generating native code`, 0 warnings de trimming/AOT.

## Tests

```bash
dotnet test --solution CleanWebApiTemplate.slnx
```

Resultado: 211 tests, 211 passed, 0 failed, 0 skipped, 16,5 s.

| Proyecto | Tests |
|---|---:|
| `Api.UnitTests` | 113 |
| `Api.ComponentTests` | 54 |
| `Api.FunctionalTests` | 11 |
| `OpenApiExamples.UnitTests` | 33 |

## Formato

```bash
dotnet format whitespace CleanWebApiTemplate.slnx --verify-no-changes
dotnet format style CleanWebApiTemplate.slnx --verify-no-changes
dotnet format analyzers CleanWebApiTemplate.slnx --verify-no-changes
dotnet format analyzers CleanWebApiTemplate.slnx --verify-no-changes --exclude-diagnostics IL2026 IL3050
```

| Comando | Resultado |
|---|---|
| whitespace | ✅ |
| style | ✅ |
| analyzers | ❌ 18 `IL2026`/`IL3050` en `MapGet`/`MapPost`/`MapPut`/`MapDelete` |
| analyzers sin AOT | ✅ |

Los errores de `analyzers` son falsos positivos de la herramienta: no ejecuta el source
generator de request delegates. `dotnet build` y `dotnet publish` AOT son los gates válidos.

## Analyzers

```bash
dotnet build CleanWebApiTemplate.slnx \
  -p:AnalysisMode=Recommended -p:AnalysisLevel=latest \
  -p:TreatWarningsAsErrors=false -p:EnforceCodeStyleInBuild=true
```

Resultado: 0 warnings.

```bash
dotnet build CleanWebApiTemplate.slnx \
  -p:AnalysisMode=All -p:AnalysisLevel=latest-all \
  -p:TreatWarningsAsErrors=false -p:EnforceCodeStyleInBuild=true
```

Resultado: 104 CA1707, 68 CA1515, 48 CA2007, 30 CA2234, 26 CA1062, 12 CA1305, 8 CA1819,
8 CA1307, 4 CA2000, 4 CA1715, 2 CA1859, 2 CA1852, 2 CA1848, 2 CA1816, 2 CA1716, 2 CA1310,
2 CA1308, 2 CA1052, 2 CA1012. La mayoría son ruido de tests/ASP.NET Core; las reglas reales
para `src/` son CA1305, CA1310, CA2234, CA1062 y CA1848.

## Cobertura

```bash
dotnet-coverage collect -f cobertura -o coverage-unit.xml \
  dotnet test --project tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj --no-build
dotnet-coverage collect -f cobertura -o coverage-component.xml \
  dotnet test --project tests/CleanWebApiTemplate.Api.ComponentTests/CleanWebApiTemplate.Api.ComponentTests.csproj --no-build
dotnet-coverage collect -f cobertura -o coverage-functional.xml \
  dotnet test --project tests/CleanWebApiTemplate.Api.FunctionalTests/CleanWebApiTemplate.Api.FunctionalTests.csproj --no-build
dotnet-coverage merge -f cobertura -o coverage-merged.xml \
  coverage-unit.xml coverage-component.xml coverage-functional.xml
reportgenerator -reports:coverage-merged.xml -targetdir:coverage-src \
  -reporttypes:'TextSummary;MarkdownSummaryGithub;Cobertura' \
  -assemblyfilters:'+CleanWebApiTemplate.Api;+CleanWebApiTemplate.Application;+CleanWebApiTemplate.Domain;+CleanWebApiTemplate.Infrastructure;+CleanWebApiTemplate.ServiceDefaults' \
  -filefilters:'-**/obj/**;-**/bin/**'
```

Resultado: 82,7% líneas / 79,7% ramas / 94,8% métodos en `src/`.
Detalle en `coverage-summary.txt` y `coverage-summary.md`.

## Cobertura tras Batch 5

```bash
npm run coverage
# equivalente a: bash scripts/coverage.sh
```

Resultado: **98,0% líneas / 95,7% ramas** en `src/`; Api **98,8% / 95,9%**. Umbrales
aplicados y cumplidos. Detalle en `coverage-summary-batch5.txt` y
`coverage-summary-batch5.md`.

## Paquetes

```bash
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
dotnet list CleanWebApiTemplate.slnx package --deprecated
dotnet list CleanWebApiTemplate.slnx package --outdated
```

| Comprobación | Resultado |
|---|---|
| Vulnerables | ✅ Ninguno |
| Deprecados | ✅ Ninguno |
| Outdated | ⚠️ Varias actualizaciones; ver `package-audit.md` |

## Secretos

```bash
git log -p --all -G'BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY'
git log -p --all -G'API_KEY'
git log -p --all -G'(?i)(password|secret|token)\s*[=:]\s*["'\''][^"'\'']{8,}'
```

Resultado: sin claves privadas ni contraseñas/tokens literales. `API_KEY` aparece solo como
nombre de configuración/constante, no como valor. No se ejecutó `gitleaks`.

## Entorno

```bash
dotnet --info
dotnet tool list --local
dotnet tool list --global
docker info
aspire --version
node --version
npm --version
git --version
```

Resultado: ver `00-baseline.md`.
