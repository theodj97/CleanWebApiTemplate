# 15 — Batch 6: hooks y guardarraíles

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Cierre de los guardarraíles locales acordados en el Batch 6:

- `pre-push` con build, formato, analyzers, **todos** los tests y paquetes vulnerables;
- `commit-msg` con convención;
- `pre-commit` con gitleaks + formato staged;
- política de `dotnet format analyzers` (excluir IL2026/IL3050);
- `AnalysisMode=Recommended` en código de producción;
- centralización de `Directory.Build.props`, limpieza de paquetes y ampliación de
  `.editorconfig`/`.gitattributes`;
- automatización local de SEC-015 (gitleaks + auditoría de paquetes); CI sigue fuera de
  alcance.

No se ha creado ni modificado nada de CI/CD (prohibido por el repositorio).

## Cambios

### Hooks

- `.husky/pre-commit`: ejecuta `gitleaks-staged` y `format-staged`.
- `.husky/commit-msg`: valida el asunto con
  `^(feat|fix|refactor|docs|test|chore|build|perf|revert)(\([a-z0-9-]+\))?: .{1,72}$`.
  Permite `Merge`, `Revert`, `fixup!` y `squash!`.
- `.husky/pre-push`: ejecuta `scripts/pre-push.sh`.
- `.husky/task-runner.json`: tareas `gitleaks-staged` y `dotnet-format-staged` (pre-commit)
  y `pre-push`.
- `.husky/format-staged.sh`: lista NUL + `xargs -0` (rutas con espacios), `--no-restore` en
  las tres llamadas y verificación IDE0060 aislada con `--diagnostics IDE0060`.
- Se mantiene el comportamiento de no hacer `git add` automático.

### Scripts

- `scripts/pre-push.sh`: build, `format whitespace/style/analyzers`, todos los tests y
  paquetes vulnerables. Es el mismo comando que `npm run verify`.
- `scripts/check-vulnerable-packages.sh`: usa `dotnet list package --format json` y falla si
  aparece cualquier vulnerabilidad (el comando devuelve 0 aunque haya hallazgos).
- `scripts/commit-msg.sh`: validación de la convención.
- `scripts/install-gitleaks.sh`: descarga gitleaks 8.30.1 para linux/darwin/windows,
  verifica SHA-256 y lo deja en `.tools/gitleaks` (ignorado por git).
- `scripts/gitleaks-staged.sh`: instala el binario si falta y ejecuta
  `gitleaks git --staged`.
- `.gitleaks.toml`: extiende las reglas por defecto; los tres fixtures de test con claves
  simétricas falsas llevan `gitleaks:allow` en la propia línea, sin excluir el fichero entero.

### Analyzers

- `Directory.Build.props` centraliza `TargetFramework`, `ImplicitUsings`, `Nullable`,
  `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `EnableNETAnalyzers`, `NuGetAudit` y
  `NuGetAuditMode`.
- `src/Directory.Build.props` añade `AnalysisLevel=latest` + `AnalysisMode=Recommended` solo
  a `src/`; tests y `local/` conservan el conjunto por defecto del SDK.
- Correcciones en `src/`:
  - CA1304/CA1305/CA1311: cultura explícita en `Ulid.Parse`, `StringBuilder.Append` y
    `ToLowerInvariant`.
  - CA1310: `EndsWith(..., StringComparison.Ordinal)`.
  - CA1715: parámetros genéricos `TDto`/`TResponse` en `IBaseResponse`.
  - CA1805: inicializaciones redundantes eliminadas.
  - CA1852: `OpenBaoJsonSerializerContext` sealed.
  - CA2208: excepción de modo de autenticación no válido.
  - CA1848: `LoggerMessage` source-generated en `GlobalExceptionHandler`.
- Supresiones documentadas en `.editorconfig`: CA1000 (factorías de `Result<T>`), CA1716
  (`Error`, QUAL-016/P3), CA1707 en `Constants.cs` (QUAL-009/P3), reglas de tests y logging
  del sidecar local (QUAL-015/B11).

### Paquetes y configuración

- Eliminados de `Directory.Packages.props`: `Microsoft.AspNetCore.Http`,
  `Microsoft.AspNetCore.Http.Abstractions`, `Microsoft.Testing.Platform` y
  `Microsoft.Testing.Extensions.CodeCoverage` (sin referencias).
- `.editorconfig`: secciones para Markdown (sin recortar espacios finales) y scripts shell.
- `.gitattributes`: `* text=auto eol=lf`, binarios y scripts de Windows con CRLF.
- `.gitignore`: `.tools/`.
- `package.json`: scripts `test`, `verify`, `hooks:install` y `gitleaks:install`.

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ AOT, 0 warnings |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 313 tests, 0 failed |
| `dotnet format whitespace/style --verify-no-changes` | ✅ |
| `dotnet format analyzers --exclude-diagnostics IL2026 IL3050` | ✅ |
| `bash scripts/pre-push.sh` | ✅ gates completos |
| `scripts/check-vulnerable-packages.sh` | ✅ sin paquetes vulnerables |
| `gitleaks dir . --config .gitleaks.toml` | ✅ sin leaks (los fixtures llevan `gitleaks:allow`) |

### Tests

| Proyecto | Tests |
|---|---:|
| `Api.UnitTests` | 145 |
| `Api.ComponentTests` | 115 |
| `Api.FunctionalTests` | 11 |
| `ArchitectureTests` | 9 |
| `OpenApiExamples.UnitTests` | 33 |
| **Total** | **313** |

## Pendiente / fuera de alcance

- **TOOL-024**: overrides de Windows/output en `task-runner.json`; Git for Windows ya aporta
  `sh` y no se ha necesitado.
- **semgrep/trivy/ZAP/Stryker**: siguen siendo herramientas manuales o de CI.
- **B11**: lock files de NuGet, `LoggerMessage` en el sidecar local y limpieza de paquetes
  pendiente.
- **TEST-007/010/011/013/014/015**: batches posteriores.
