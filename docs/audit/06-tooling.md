# 06 — Tooling y developer experience

## Estado actual

| Herramienta | Estado | Comentario |
|---|---|---|
| .NET SDK | 10.0.113 | `global.json` pide 10.0.100 con `latestFeature`. |
| Husky.Net | 0.9.1 local | Última versión; solo `pre-commit`. |
| `dotnet format` | Integrado | `whitespace` y `style` pasan; `analyzers` falla por IL2026/IL3050. |
| Node / npm | 24.22 / 11.19 | `package.json` sin dependencias reales. |
| Docker | 29.1.3 | Component/functional tests funcionan. |
| ReportGenerator | 5.5.11 local | Fijado en `.config/dotnet-tools.json` y usado por `scripts/coverage.sh`. |
| dotnet-coverage | 18.12.0 local | Fijado en `.config/dotnet-tools.json` y usado por `scripts/coverage.sh`. |
| gitleaks / semgrep / trivy / ZAP / Stryker | No instalados | Se proponen comandos; no se ejecutaron. |

## Husky: lo que hay

```text
.husky/
├── _/husky.sh                 # runtime generado
├── pre-commit                 # dotnet husky run --group pre-commit
├── task-runner.json           # una tarea: format-staged.sh sobre **/*.cs staged
└── format-staged.sh           # whitespace + style + verificación IDE0060
```

Lo que hace bien:

- Solo formatea los `.cs` staged, no toda la solución.
- No hace `git add` automático: si `dotnet format` cambia algo, aborta para que revises.
- Bloquea `IDE0060` (parámetro sin usar) sin modificar el archivo.
- Detecta cambios sin stagear en los mismos archivos.
- Permite saltar con `HUSKY=0` o `--no-verify`.

Problemas:

| ID | Problema | Evidencia |
|---|---|---|
| TOOL-001 | Solo `pre-commit` y solo `.cs` | `.husky/task-runner.json` |
| TOOL-002 | `--include $staged` y `git diff -- $staged` rompen con rutas con espacios | `format-staged.sh:32,51-52` |
| TOOL-021 | Las dos primeras llamadas a `dotnet format` no usan `--no-restore` | `format-staged.sh:32-36` |
| TOOL-023 | El mensaje de IDE0060 es engañoso con `--severity error` | `format-staged.sh:67-71` |
| TOOL-024 | `task-runner.json` no tiene overrides de Windows/output | `task-runner.json` |
| TOOL-025 | No hay `commit-msg` | `.husky/` |
| TOOL-026 | No hay `pre-push` con build/tests | `.husky/` |

## Husky: configuración recomendada

### `pre-commit` (rápido, < 5 s)

- Formatear `.cs` staged con `dotnet format whitespace/style` y `--no-restore`.
- Ejecutar `dotnet format analyzers` **solo si** se excluyen IL2026/IL3050 (falsos positivos de
  minimal APIs) o no se usa en este repo.
- Escanear secretos en staged con `gitleaks protect --staged` (si se instala).
- Formatear JSON/YAML/MD solo si se adopta Prettier/markdownlint; no añadir Node por solo esto.
- No ejecutar build ni tests: son lentos y van en `pre-push`.

### `commit-msg` (rápido)

Validar convención de commits:

```text
^(feat|fix|refactor|docs|test|chore|build|perf|revert)(\([a-z0-9-]+\))?: .{1,72}$
```

Permitir `Merge`, `Revert`, `fixup!` y `squash!`. Sin dependencias: un script `sh` o un `.csx`
de Husky.

### `pre-push` (medio, 1-3 min)

- `dotnet build CleanWebApiTemplate.slnx --no-restore` con warnings-as-errors.
- `dotnet format whitespace/style --verify-no-changes --no-restore`.
- `dotnet test --project tests/CleanWebApiTemplate.Api.UnitTests/... --no-build`.
- `dotnet list package --vulnerable --include-transitive`.
- Tests de componente/functional solo con `HUSKY_FULL=1` (Docker, ~30 s; aceptable si se
  quiere, pero no en cada push por defecto).

### `post-checkout` / `post-merge`

- `dotnet tool restore` si cambia `.config/dotnet-tools.json`.
- `dotnet husky install` si falta `.husky/_`.
- `dotnet restore` si cambian `Directory.Packages.props`/csproj.

### Ejemplo de `task-runner.json`

```json
{
  "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
  "tasks": [
    {
      "name": "format-staged",
      "group": "pre-commit",
      "command": "sh",
      "args": [".husky/format-staged.sh"],
      "include": ["**/*.cs"],
      "filteringRule": "staged"
    },
    {
      "name": "build",
      "group": "pre-push",
      "command": "dotnet",
      "args": ["build", "CleanWebApiTemplate.slnx", "--no-restore"]
    },
    {
      "name": "unit-tests",
      "group": "pre-push",
      "command": "dotnet",
      "args": ["test", "--project", "tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj", "--no-build"]
    },
    {
      "name": "vulnerable-packages",
      "group": "pre-push",
      "command": "dotnet",
      "args": ["list", "CleanWebApiTemplate.slnx", "package", "--vulnerable", "--include-transitive"]
    }
  ]
}
```

Para `commit-msg`, Husky.Net puede ejecutar un script propio; no hace falta commitlint.

## `format-staged.sh`: mejoras concretas

1. Usar `git diff --cached --name-only -z --diff-filter=ACMR -- '*.cs'` y `xargs -0` para
   soportar rutas con espacios.
2. Pasar `--no-restore` a las tres llamadas.
3. Separar la verificación de `IDE0060` de otras reglas error-severity:
   `dotnet format style --diagnostics IDE0060 --severity error --verify-no-changes`.
4. Mantener el comportamiento de “no hacer `git add`”: es correcto.
5. Considerar un `.csx` de Husky para cross-platform, o mantener `sh` (Git Bash en Windows).

## Analyzers

### Estado

- `dotnet build` normal: 0 warnings.
- `AnalysisMode=Recommended`: 0 warnings.
- `AnalysisMode=All`: mucho ruido (CA1707, CA1515, CA2007, CA2234, CA1062, CA1305, CA1819...).
- `dotnet format analyzers`: falla con IL2026/IL3050 en `Map*` por no ejecutar el source
  generator de request delegates.

### Recomendación

1. `AnalysisMode=Recommended` + `AnalysisLevel=latest` en `Directory.Build.props`.
2. Suprimir por carpeta:
   - tests: CA1707, CA1515, CA2007, CA2000, CA2234;
   - DTOs: CA1819;
   - ASP.NET Core: CA2007.
3. Corregir las reglas reales en `src/`: CA1305, CA1310, CA2234, CA1062 en entry points,
   CA1848 si se quiere.
4. Añadir `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt`:
   ```text
   System.Reflection.Assembly.Load
   System.Activator.CreateInstance
   System.Type.GetType
   System.Reflection.MethodInfo.MakeGenericMethod
   System.Text.Json.JsonSerializer.Serialize(System.Object)
   System.Text.Json.JsonSerializer.Deserialize(System.String,System.Type)
   ```
5. No añadir `dotnet format analyzers` al pre-commit sin excluir IL2026/IL3050; usar
   `dotnet build`/`publish` como gate AOT.

## Cobertura

- `dotnet-coverage` 18.12.0 y ReportGenerator 5.5.11 están fijados en
  `.config/dotnet-tools.json`.
- `scripts/coverage.sh` recoge unit + component + functional, fusiona, genera el informe
  filtrado a `src/` y aplica umbrales.
- `npm run coverage` es el punto de entrada documentado.
- `Microsoft.Testing.Extensions.CodeCoverage` sigue declarado sin referencia; no se usa
  porque la fusión con `dotnet-coverage` ya es reproducible.

## Seguridad

No hay gitleaks/semgrep/trivy. Recomendación:

- `gitleaks protect --staged` en pre-commit.
- `gitleaks detect --log-opts=--all` una vez y en pre-push.
- `dotnet list package --vulnerable --include-transitive` en pre-push.
- `semgrep --config p/csharp --config p/security-audit .` manual.
- `trivy fs --scanners vuln,secret .` manual.
- ZAP baseline contra el AppHost local antes de un release.

## `Directory.Build.props` propuesto

Centralizar:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <AnalysisLevel>latest</AnalysisLevel>
  <AnalysisMode>Recommended</AnalysisMode>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>
</PropertyGroup>
```

Cuidado: `TargetFramework` centralizado puede romper proyectos que necesiten otro; para este
repo todos son `net10.0`. Los tests pueden mantener `IsTestProject`/`IsPackable`.

## `Directory.Packages.props`

- Eliminar `Microsoft.AspNetCore.Http` y `.Abstractions` de Domain.
- Referenciar `Microsoft.Testing.Extensions.CodeCoverage` en los proyectos de test o eliminarlo.
- Eliminar `Microsoft.Testing.Platform` si MTP ya lo trae.
- Añadir `Microsoft.CodeAnalysis.BannedApiAnalyzers` y `ArchUnitNET` (test-only).
- Añadir `RestorePackagesWithLockFile` y commitear `packages.lock.json` si se quiere
  reproducibilidad total.
- Evaluar `Microsoft.OpenApi` 3.x y `xunit.v3` 4.x como migraciones separadas.

## `package.json`

Opciones:

1. **Eliminarlo** si no se usa Node. Husky.Net no lo necesita. `package-lock.json` vacío sobra.
2. **Convertirlo en tooling real**: `prettier` + `markdownlint-cli2` para JSON/YAML/MD y
   scripts `format:json`, `lint:md`. Requiere `npm ci` y mantenimiento.

Mi recomendación: opción 1 salvo que se quiera formatear Markdown/JSON de forma estricta. El
repo ya tiene `dotnet format` para C#; añadir Node solo por JSON/MD no compensa en un template
.NET.

## Template tooling

Ver `07-template-docs.md`. Resumen:

- `template.json` incompleto.
- GUIDs fijos.
- Incluye `coverage-report/` y `.agents/`.
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
dotnet test --solution CleanWebApiTemplate.slnx --no-build
dotnet list package --vulnerable --include-transitive
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
gitleaks detect --source . --log-opts=--all
```

## Conclusión

Husky está bien instalado y el script de formato tiene una intención excelente, pero el
conjunto de hooks es demasiado pequeño para un template serio. La mejora de mayor retorno es
añadir `pre-push` con build + unit tests + paquetes vulnerables y `commit-msg` con convención;
la segunda es cablear cobertura y analyzers; la tercera es añadir escaneo de secretos.
