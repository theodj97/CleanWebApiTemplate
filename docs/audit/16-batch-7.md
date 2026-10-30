# 16 — Batch 7: template y documentación

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Cierre de los hallazgos P1 de template y documentación:

- TPL-001/002: metadatos del template y `shortName` en minúsculas.
- TPL-003/004: `.gitignore` y README en el proyecto generado; artefactos locales excluidos.
- TPL-005: `UserSecretsId` generados en cada `dotnet new`.
- TPL-006: símbolos y post-actions.
- TPL-007: smoke test automatizado de `dotnet new`.
- DOC-001: README raíz.
- DOC-002: referencias rotas de `AGENTS.md`.
- DOC-003/004/005/006: drift de `local/local-development.md`.
- DOC-007: semántica de títulos y filtros alineada en la documentación.
- DOC-010: README propio en el proyecto generado.

No se ha creado ni modificado nada de CI/CD (prohibido por el repositorio).

## Decisiones

| Tema | Decisión |
|---|---|
| README | Separado: `README.md` documenta este repositorio; `README.template.md` se renombra a `README.md` en el proyecto generado. |
| DOC-002 | Eliminar de `AGENTS.md` la sección “Agent skills” (los documentos `docs/agents/`, `GLOSSARY.md` y `docs/adr/` ya se habían retirado en `bcbbc07`). |
| Husky | `dotnet husky install` necesita un repositorio Git. Los post-actions **no** ejecutan `git init`; se documenta como paso manual y el smoke test lo ejecuta y verifica. |
| Smoke test | Gate manual/local (`scripts/template-smoke.sh`), no cableado a `pre-push` por coste. |

## Cambios

### `.template.config/template.json`

- `name`, `description`, `classifications` (`Web`, `Web API`, `Clean Architecture`, `Aspire`),
  `tags.framework = net10.0` y `shortName = ["clean-webapi", "cleanwebapi"]`.
- Símbolos:
  - `skipRestore` (bool), expuesto como `--no-restore` en `dotnetcli.host.json`.
  - `ApiUserSecretsId` y `AppHostUserSecretsId`: generador `guid` (formato `d`) que sustituye
    los GUID fijos de `Api.csproj` y `AppHost.csproj`.
- Las formas en minúsculas de `sourceName` (`cleanwebapitemplate` en `package.json`, rutas de
  OpenBao y tests) se sustituyen solas: no hace falta un símbolo `casing`.
- `exclude`: `.agents/**`, `.task-skill/**`, `.tools/**`, `.vs/**`, `.idea/**`,
  `skills-lock.json`, `coverage-report/**`, `docs/audit/**`, `scripts/template-smoke.sh`,
  `**/TestResults/**`, además de los existentes. `.gitignore` ya no se excluye.
- `rename`: `README.template.md` → `README.md` (el `README.md` raíz se sigue excluyendo).
- `primaryOutputs`: la solución, para que el restore actúe sobre ella.
- `postActions`:
  1. Restore NuGet (`210D431B-…`), condicionado a `!skipRestore`.
  2. `dotnet tool restore` con la acción “run script” (`3A7C4B45-1F5D-4A30-959A-51B88E82B5D2`),
     condicionado a `!skipRestore`. Al ser un script, `dotnet new` pide confirmación salvo con
     `--allow-scripts yes`.
  3. `chmod +x` (`cb9a6cf3-…`) de `scripts/*.sh` y los hooks, fuera de Windows.

El ejemplo de la auditoría usaba `3A7C4B69-…` como `actionId`; no corresponde a ningún
procesador. Se corrige en `07-template-docs.md`.

### Smoke test: `scripts/template-smoke.sh`

1. Instala el template en un hive temporal (`--debug:custom-hive`), sin tocar la lista global.
2. Genera `Sample` (con restore y `--allow-scripts yes`) y `Sample2` (`--no-restore`).
3. Comprueba: `README.md`, `.gitignore`, `Sample.slnx`, `AGENTS.md` y el manifiesto de tools
   presentes; `coverage-report`, `.agents`, `.tools`, `.task-skill`, `skills-lock.json`,
   `docs/audit`, `README.template.md`, el propio script, `.template.config` y `.husky/_`
   ausentes; README con el nombre del proyecto; ningún `CleanWebApiTemplate` (cualquier
   capitalización) en nombres ni contenido; `package.json` en minúsculas; cuatro
   `UserSecretsId` válidos, distintos entre sí y distintos de los del template; hooks y
   scripts ejecutables.
4. `git init` + `dotnet tool restore` + `dotnet husky install` y comprobación de
   `core.hooksPath`.
5. `dotnet build`, tests sin contenedores (unit, architecture y sidecar local) y publish
   Native AOT con el RID de la máquina.

Opciones: `--keep` conserva la salida y `--no-aot` omite el publish. El script lee los GUID del
template de los `.csproj` en vez de duplicarlos (gitleaks marcaba los literales como
`generic-api-key`).

### Documentación

- `README.md` (raíz): qué es el template, uso de `dotnet new` y opciones, post-actions, trabajo
  sobre el repositorio, smoke test e índice de documentación.
- `README.template.md`: README del proyecto generado (quickstart con `git init`/Husky,
  arquitectura y regla de dependencias, AOT, configuración y secretos, contrato de la API,
  tests, hooks y troubleshooting). Contrato verificado contra el código: `DELETE` devuelve
  `200` con `true`/`false`, títulos con trim y únicos por owner (`409`), filtro `contains`
  case-insensitive con `%`, `_` y `\` escapados, y `email` ausente → `403`.
- `AGENTS.md`: eliminada la sección “Agent skills” (DOC-002).
- `local/local-development.md`:
  - SDK `10.0.1xx` según `global.json` (DOC-004) y Docker `29.x` sin versión exacta (DOC-003).
  - `/health` devuelve el estado agregado; diagnóstico con `/health/api` y
    `/health/postgresDb` (DOC-005).
  - `API_KEY` corta: la API no arranca y muestra el mensaje real (DOC-006; la validación es
    de Batch 2).
  - Reglas de título y filtro (DOC-007).
  - La sección “AI coding agents” pasa a “Diagnostics”: `.agents/` no forma parte del proyecto
    generado.
- `tests/README.md`: versión del SDK, fila de `ArchitectureTests` y sección de cobertura con
  umbrales.
- `package.json`: `private`, descripción corta y sin `repository`/`bugs`/`homepage`, que en un
  proyecto generado apuntarían a `theodj97/<Nombre>`; `package-lock.json` alineado.
- `scripts/coverage.sh`: el mensaje final apuntaba a un `index.html` que no se genera; ahora
  apunta a `Summary.txt`.

## Verificación

| Gate | Resultado |
|---|---|
| `bash scripts/pre-push.sh` (build, formato, analyzers, tests, paquetes) | ✅ 0 warnings, 313 tests, sin vulnerables |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ AOT, 0 warnings |
| `bash scripts/template-smoke.sh` | ✅ 34 comprobaciones, build 0 warnings, 187 tests, AOT |
| `dotnet test --solution Sample.slnx` en el proyecto generado (Docker) | ✅ 313 tests |
| gitleaks sobre los ficheros nuevos | ✅ sin leaks |

Evidencia: [`evidence/batch-7-gates.txt`](evidence/batch-7-gates.txt).

## Pendiente / fuera de alcance

- **TPL-008** (P2): flags opcionales (`--sample`, `--openbao`, `--auth`).
- **TPL-009** (P2): `LICENSE` y `CONTRIBUTING.md`; requiere decidir la licencia.
- `aspire start` en el proyecto generado no se ha probado de forma interactiva; los tests de
  componente y funcionales del proyecto generado sí arrancan su AppHost con contenedores.
- **Batch 8**: borde de seguridad (headers, forwarded headers, rate limiting, límite de body,
  OpenBao HTTPS, CORS).
