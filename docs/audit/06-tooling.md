# 06 — Tooling y developer experience

## Estado actual

| Herramienta | Estado | Comentario |
|---|---|---|
| .NET SDK | 10.0.113 | `global.json` pide 10.0.100 con `latestFeature`. |
| Husky.Net | 0.9.1 local | `pre-commit`, `commit-msg` y `pre-push`. |
| `dotnet format` | Integrado | `whitespace`, `style` y `analyzers` pasan (estos últimos excluyendo IL2026/IL3050). |
| Node / npm | 24.22 / 11.19 | `package.json` solo con scripts; sin dependencias. |
| Docker | 29.1.3 | Component/functional tests funcionan; `pre-push` los ejecuta. |
| ReportGenerator | 5.5.11 local | Fijado en `.config/dotnet-tools.json` y usado por `scripts/coverage.sh`. |
| dotnet-coverage | 18.12.0 local | Fijado en `.config/dotnet-tools.json` y usado por `scripts/coverage.sh`. |
| gitleaks | 8.30.1 fijado | `scripts/install-gitleaks.sh` (SHA-256) y `.husky` pre-commit. |
| semgrep / trivy / ZAP / Stryker | No instalados | Se proponen comandos; no se ejecutaron. |

## Husky: estado tras Batch 6

```text
.husky/
├── _/husky.sh                 # runtime generado
├── pre-commit                 # gitleaks staged + format-staged
├── pre-push                   # scripts/pre-push.sh
├── commit-msg                 # scripts/commit-msg.sh
├── task-runner.json           # tareas de pre-commit y pre-push
└── format-staged.sh           # whitespace + style + verificación IDE0060
scripts/
├── commit-msg.sh              # convención del asunto
├── gitleaks-staged.sh         # escaneo del índice
├── install-gitleaks.sh        # binario fijado + SHA-256
├── check-vulnerable-packages.sh
└── pre-push.sh                # gates completos
```

- **`pre-commit`** (< 5 s): escanea el índice con gitleaks y formatea solo los `.cs` staged.
  No hace `git add`: si `dotnet format` toca algo, aborta para revisar el diff. Detecta
  cambios sin stagear en esos mismos archivos.
- **`commit-msg`**: valida el asunto con
  `^(feat|fix|refactor|docs|test|chore|build|perf|revert)(\([a-z0-9-]+\))?: .{1,72}$`,
  permitiendo `Merge`, `Revert`, `fixup!` y `squash!`.
- **`pre-push`**: `scripts/pre-push.sh` ejecuta build, `format whitespace/style/analyzers`,
  **todos** los tests (unit, component y functional) y la auditoría de paquetes
  vulnerables. Los component/functional necesitan Docker.
- Se puede saltar con `HUSKY=0` o `--no-verify`. Instalación en un clon nuevo:
  `npm run hooks:install` (o `dotnet tool restore && dotnet husky install`).

Problemas del Batch 6:

| ID | Estado |
|---|---|
| TOOL-001 | ✅ `pre-commit`, `commit-msg` y `pre-push`. |
| TOOL-002 | ✅ Lista NUL + `xargs -0`; soporta rutas con espacios. |
| TOOL-021 | ✅ Las tres llamadas usan `--no-restore`. |
| TOOL-023 | ✅ La verificación usa `--diagnostics IDE0060`; el mensaje ya no acusa a otras reglas. |
| TOOL-024 | ⚠️ Sin overrides de Windows/output. Git for Windows aporta `sh`; no se ha necesitado. |
| TOOL-025 | ✅ `commit-msg` convencional. |
| TOOL-026 | ✅ `pre-push` con build, formato, analyzers, todos los tests y paquetes. |

### `pre-push` (implementado)

1. `dotnet build CleanWebApiTemplate.slnx` (con restore y `TreatWarningsAsErrors`).
2. `dotnet format whitespace/style --verify-no-changes --no-restore`.
3. `dotnet format analyzers --verify-no-changes --exclude-diagnostics IL2026 IL3050`.
4. `dotnet test --solution CleanWebApiTemplate.slnx --no-build` (todos los proyectos).
5. `scripts/check-vulnerable-packages.sh` (falla si el JSON trae vulnerabilidades).

### `format-staged.sh` (implementado)

1. `git diff --cached --name-only -z --diff-filter=ACMR -- '*.cs'` + `xargs -0`.
2. `--no-restore` en whitespace, style y la verificación.
3. Verificación IDE0060 aislada:
   `dotnet format style --diagnostics IDE0060 --severity error --verify-no-changes`.
4. Se mantiene “no hacer `git add`”.
5. Script `sh` (Git Bash en Windows).

## Analyzers

### Estado tras Batch 6

- `dotnet build` normal: 0 warnings.
- `AnalysisMode=Recommended` + `AnalysisLevel=latest` en `src/`: 0 warnings.
- `AnalysisMode=All` sigue siendo ruido (CA1707, CA1515, CA2007, CA2234, CA1062, CA1819...).
- `dotnet format analyzers --exclude-diagnostics IL2026 IL3050`: ✅.
- IL2026/IL3050 en `Map*` son falsos positivos: `dotnet format analyzers` no ejecuta el
  source generator de request delegates. El gate AOT real es `dotnet publish`.

### Decisiones

1. `AnalysisMode=Recommended` + `AnalysisLevel=latest` se aplica solo a `src/` mediante
   `src/Directory.Build.props`; tests y `local/` conservan el conjunto por defecto del SDK.
2. Reglas corregidas en `src/`: CA1304/CA1305/CA1311 (cultura), CA1310 (`EndsWith`),
   CA1715 (parámetros genéricos), CA1805 (inicialización redundante), CA1852 (tipo sealed),
   CA2208 (`ArgumentOutOfRangeException`) y CA1848 (LoggerMessage en `GlobalExceptionHandler`).
3. Supresiones documentadas en `.editorconfig`:
   - CA1000: factorías estáticas de `Result<T>` (API pública intencionada).
   - CA1716: `Error` es el concepto del dominio; rename en QUAL-016 (P3).
   - CA1707 en `Constants.cs`: claves de configuración; rename en QUAL-009 (P3).
   - tests: CA1707/CA1861/CA1515/CA2007/CA2000/CA2234.
   - `local/openapi`: CA1848/CA1873; migración en QUAL-015 (Batch 11).
4. `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt` ya estaban desde Batch 4b.
5. `dotnet format analyzers` va en `pre-push` (no en `pre-commit`) y excluye IL2026/IL3050.

## Cobertura

- `dotnet-coverage` 18.12.0 y ReportGenerator 5.5.11 están fijados en
  `.config/dotnet-tools.json`.
- `scripts/coverage.sh` recoge unit + component + functional, fusiona, genera el informe
  filtrado a `src/` y aplica umbrales.
- `npm run coverage` es el punto de entrada documentado.
- `Microsoft.Testing.Extensions.CodeCoverage` se eliminó de `Directory.Packages.props`: no se
  usaba y la fusión con `dotnet-coverage` ya es reproducible.

## Seguridad

gitleaks está integrado desde Batch 6:

- `scripts/install-gitleaks.sh` descarga la versión fijada (8.30.1) y verifica su SHA-256.
- `.husky` `pre-commit` ejecuta `gitleaks git --staged` sobre el índice; `.gitleaks.toml`
  extiende las reglas por defecto y los fixtures de test con claves falsas llevan
  `gitleaks:allow` en la línea.
- `.husky` `pre-push` ejecuta `scripts/check-vulnerable-packages.sh`, que falla si
  `dotnet list package --vulnerable --include-transitive` encuentra algo.
- `semgrep --config p/csharp --config p/security-audit .`, `trivy fs --scanners vuln,secret .`
  y ZAP baseline contra el AppHost local siguen siendo comprobaciones manuales.

## `Directory.Build.props` (implementado)

`Directory.Build.props` centraliza:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>
</PropertyGroup>
```

`src/Directory.Build.props` importa el anterior y añade `AnalysisLevel=latest` +
`AnalysisMode=Recommended` solo al código de producción. Tests y `local/` conservan el
conjunto por defecto del SDK. Los `.csproj` mantienen lo específico: AOT, Aspire, tests,
documentación XML y `UserSecretsId`.

`TargetFramework` centralizado no rompe nada porque todos los proyectos son `net10.0`.

## `Directory.Packages.props`

- ✅ Eliminados `Microsoft.AspNetCore.Http` y `.Abstractions`.
- ✅ Eliminado `Microsoft.Testing.Extensions.CodeCoverage` (sin referencia).
- ✅ Eliminado `Microsoft.Testing.Platform` (MTP ya lo trae por xunit).
- ✅ `Microsoft.CodeAnalysis.BannedApiAnalyzers` y `ArchUnitNET` ya estaban desde Batch 4b.
- Pendiente (B11): `RestorePackagesWithLockFile` + `packages.lock.json`.
- Migraciones separadas: `Microsoft.OpenApi` 3.x y `xunit.v3` 4.x.

## `package.json`

Se mantiene como manifiesto de scripts, sin dependencias Node:

```json
"format", "format:check", "coverage", "test", "verify", "hooks:install", "gitleaks:install"
```

`npm run verify` ejecuta los mismos gates que `pre-push`. No se añade Prettier ni
markdownlint: el repo usa `dotnet format` para C# y no compensa meter Node solo para JSON/MD.

## Template tooling

Ver `07-template-docs.md`. Resumen:

- `template.json` incompleto.
- GUIDs fijos.
- Incluye `coverage-report/`, `.agents/` y `.tools/` (binario de gitleaks) si no se excluyen.
- Excluye `.gitignore`/README.
- Sin post-actions.
- Sin smoke test.

## CI

El repositorio prohíbe crear/modificar CI. La auditoría deja los comandos listos para que el
usuario los configure manualmente:

```bash
dotnet restore
dotnet build CleanWebApiTemplate.slnx --no-restore
dotnet format whitespace/style --verify-no-changes --no-restore
dotnet format analyzers --verify-no-changes --no-restore --exclude-diagnostics IL2026 IL3050
dotnet test --solution CleanWebApiTemplate.slnx --no-build
dotnet list package --vulnerable --include-transitive
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
gitleaks git --log-opts=--all --redact --no-banner
```

## Conclusión

Los hooks cubren ya el ciclo completo: `pre-commit` formatea y bloquea secretos, `commit-msg`
valida la convención y `pre-push` ejecuta build, formato, analyzers, **todos** los tests y la
auditoría de paquetes. gitleaks está fijado con checksum y se instala solo la primera vez.
Quedan como mejoras opcionales los overrides de Windows/output de `task-runner.json` (TOOL-024)
y las herramientas externas (semgrep, trivy, ZAP, Stryker).
