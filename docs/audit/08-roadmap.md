# 08 — Roadmap de remediación

## Principios

1. **Primero tests de regresión, después fixes.** Cada bug confirmado debe tener un test que
   falle antes del cambio y pase después.
2. **Seguridad y correctitud antes que refactor.** No mezclar un refactor de arquitectura con
   un fix de autorización.
3. **Un batch = un diff revisable.** No hacer un PR gigante con todo el roadmap.
4. **Cada batch cierra con los mismos gates**: build, AOT publish, tests, format y paquetes.
5. **Los guardarraíles van al final del batch correspondiente**, no en un “luego lo vemos”.

## P0 — Bloqueantes

No se ha confirmado ningún P0 (sin bypass de autenticación, IDOR/BOLA ni inyección SQL). Si
aparece uno durante la remediación, se para el batch y se trata aparte.

## P1 — Antes de considerar el template “listo”

### Batch 1 — Errores de entrada y contrato básico — ✅ implementado

**Estado:** implementado y verificado el 2026-10-29. Detalle en
[`09-batch-1.md`](09-batch-1.md).

**Objetivo:** que un cliente no pueda provocar 500 con un body inválido y que el contrato HTTP
sea coherente.

- BUG-001: `title: null` -> 400, no 500.
- SEC-002: JSON malformado / campo requerido ausente -> 400 ProblemDetails.
- BUG-002: `status: 0` -> 400 y no se persiste.
- BUG-006: colección vacía -> 204 (o 200 `[]` si se decide lo contrario; actualizar docs).
- BUG-007: 201 con `Location`.
- BUG-012: 404/405 con ProblemDetails.
- SEC-012: traceId en 500.

**Tests primero:**

- `Create_WithNullTitle_ReturnsBadRequest`
- `Create_WithMissingTitle_ReturnsBadRequest`
- `Create_WithMalformedJson_ReturnsBadRequest`
- `Update_WithStatusZero_ReturnsBadRequest`
- `Filter_WithNoMatches_ReturnsNoContent` (o el contrato decidido)
- `Create_WithValidRequest_ReturnsCreatedWithLocation`
- `Get_UnknownRoute_ReturnsProblemDetails`
- `MethodNotAllowed_ReturnsProblemDetails`

**Criterio de aceptación:** ningún body inválido devuelve 500; los tests anteriores pasan;
OpenAPI refleja los códigos reales.

### Batch 2 — Autenticación y claims — ✅ implementado

**Estado:** implementado y verificado el 2026-10-30. Detalle en
[`10-batch-2.md`](10-batch-2.md).

- SEC-001: `ValidAlgorithms` (HS256 en modo simétrico).
- SEC-007: validar longitud de `API_KEY` (>= 32 bytes) y coherencia issuer/audience.
- SEC-003: claim `email` ausente -> 403 (o 401), nunca 500.
- SEC-008: decidir modo de autenticación; separar OIDC/simétrico; completar o eliminar RS256.
- Tests: token sin email, token HS512, token con algoritmo inesperado, configuración inválida.

**Criterio de aceptación:** matriz de tokens ampliada; no hay 500 por claims; la configuración
inválida falla al arrancar con un mensaje claro.

### Batch 3 — Integridad de datos y reglas de negocio — ✅ implementado

**Estado:** implementado y verificado el 2026-10-30. Detalle en
[`11-batch-3.md`](11-batch-3.md).

- BUG-003: decidir unicidad global vs per-owner.
- BUG-004: índice único y manejo de conflicto.
- BUG-005: filtro “contains” o corregir docs.
- BUG-009: fechas invariantes.
- BUG-010: trim de título.
- BUG-011: off-by-one.
- BUG-014: comprobar `UpdateAsync` bool; valorar concurrencia optimista.

**Criterio de aceptación:** regla de unicidad documentada y testeada; no hay carreras
conocidas; filtros y fechas se comportan igual en cualquier cultura.

### Batch 4 — Arquitectura — ✅ implementado (4a + 4b)

**Estado 4a:** estructura repository-centric, Domain limpio, `AppSettings`/`Constants` fuera
de Domain, ASP.NET fuera de Domain y `Result<T>` sin HTTP. Detalle en
[`12-batch-4-structure.md`](12-batch-4-structure.md).

**Estado 4b:** ArchUnitNET, `BannedApiAnalyzers`, `AppConfigurationHelper`, `ICurrentUser`
(sin `BaseApiRouter`) y split de `OpenApiConfiguration` con contract test. Detalle en
[`13-batch-4b.md`](13-batch-4b.md).

- ARCH-001: mover `ITodoRepository`/`TodoFilter` a Application.
- ARCH-002: eliminar `Microsoft.AspNetCore.Http*` de Domain.
- ARCH-003: mover `AppSettings`/`Constants` fuera de Domain.
- ARCH-004: mover constraints/`TodoTable` a Domain/Application.
- ARCH-010: separar `Result<T>` de HTTP.
- ARCH-008: añadir `tests/CleanWebApiTemplate.ArchitectureTests` con ArchUnitNET.
- Añadir `BannedApiAnalyzers` + `BannedSymbols.txt`.

**Criterio de aceptación:** el grafo de dependencias cumple la regla; los architecture tests
pasan; no hay reflexión/`dynamic` en `src/`; build/AOT siguen limpios.

### Batch 5 — Cobertura de los caminos críticos — ✅ implementado

**Estado:** 313 tests verdes; cobertura de `src/` 98,0% líneas / 95,7% ramas y Api 98,8% /
95,9%; `scripts/coverage.sh` aplica umbrales estrictos. Detalle en
[`14-batch-5.md`](14-batch-5.md).

- TEST-001/005: tests de los bugs corregidos y de la matriz de auth.
- TEST-002: test del documento OpenAPI.
- TEST-003/004: tests de routing/políticas.
- TEST-006: corregir el test de colección vacía.
- TEST-012: script de cobertura + umbral.
- Objetivo mínimo: `src/` ≥ 90% líneas / ≥ 80% ramas; Api ≥ 85/75.

**Criterio de aceptación:** cobertura reproducible desde el repo; umbral aplicado; OpenAPI
contract test verde.

### Batch 6 — Hooks y guardarraíles — ✅ implementado

**Estado:** implementado y verificado el 2026-10-30. Detalle en
[`15-batch-6.md`](15-batch-6.md).

- TOOL-001/026: `pre-push` con build + formato + analyzers + **todos** los tests
  (unit, component y functional) + paquetes vulnerables.
- TOOL-025: `commit-msg` con convención (`feat`, `fix`, `refactor`, `docs`, `test`,
  `chore`, `build`, `perf`, `revert`), permitiendo `Merge`/`Revert`/`fixup!`/`squash!`.
- TOOL-002/021/023: `format-staged.sh` con lista NUL + `xargs -0` (rutas con espacios),
  `--no-restore` y verificación IDE0060 aislada.
- TOOL-003/004: `dotnet format analyzers` en `pre-push` excluyendo IL2026/IL3050; el gate
  AOT real sigue siendo `dotnet publish`.
- TOOL-005: cobertura ya cableada en Batch 5 (`npm run coverage`).
- TOOL-007: `AnalysisMode=Recommended` + `AnalysisLevel=latest` en `src/` (los banned APIs
  ya estaban desde Batch 4b).
- TOOL-013: gitleaks fijado (8.30.1) y ejecutado en `pre-commit`; auditoría de paquetes en
  `pre-push`.
- TOOL-008/009/010/011: `Directory.Build.props` centralizado, paquetes sin uso eliminados,
  `.editorconfig` y `.gitattributes` ampliados.

**Criterio de aceptación:** un commit con warning de build, test roto o paquete vulnerable
no llega al remoto; un secreto staged se bloquea. Verificado con el hook real.

### Batch 7 — Template y documentación — ✅ implementado

**Estado:** implementado y verificado el 2026-10-30. Detalle en
[`16-batch-7.md`](16-batch-7.md). TPL-008 (flags opcionales) y TPL-009 (LICENSE/CONTRIBUTING)
siguen como P2.

- TPL-001/002: metadatos del template.
- TPL-003/004: excludes correctos.
- TPL-005: GUIDs generados.
- TPL-006: post-actions.
- TPL-007: smoke test automatizado.
- DOC-001: README raíz.
- DOC-002: docs/agents/GLOSSARY/ADR o corregir referencias.
- DOC-007: alinear docs de títulos/filtros/trim.
- DOC-003/004/005/006: corregir drift local.

**Criterio de aceptación:** `dotnet new` genera un proyecto que compila, pasa unit tests,
publica AOT, tiene README/.gitignore y no arrastra artefactos locales.

## P2 — Después del template, antes de un release serio

| Batch | Contenido | IDs |
|---|---|---|
| B8 — Borde de seguridad | HSTS/security headers/ForwardedHeaders/rate limiting/límite de body/OpenBao HTTPS/CORS validation | SEC-004, SEC-005, SEC-006, SEC-007, SEC-009, SEC-016 |
| B9 — Contrato y calidad | UpdatedAt, OpenAPI contract tests, 404/405, culture, mensajes, DTOs no nullable | BUG-008, BUG-009, BUG-012, QUAL-006/007/008 |
| B10 — Calidad de tests | Mutation testing (Stryker), flakiness, test naming, `Eventually` | TEST-007, TEST-010, TEST-011, TEST-015 |
| B11 — Tooling | Lock files, NuGetAudit, package cleanup, `NpgsqlDataSource`, retry OpenBao, LoggerMessage | TOOL-009, QUAL-012/013/015 |
| B12 — ServiceDefaults | Service discovery/resiliencia si el template va a tener varios servicios | ARCH-009, QUAL-014 |

## P3 — Limpieza y nice-to-have

- QUAL-001: eliminar código muerto.
- QUAL-002: consolidar appsettings.
- QUAL-003: quitar `NoWarn` ineficaz.
- QUAL-004/012: limpiar `package.json` o darle uso.
- QUAL-005: limpiar paquetes.
- QUAL-009/010/011: constantes, HashSet, `AddEndpointsApiExplorer`.
- QUAL-016: CA1052/CA1716/CA1715/CA1310/CA2234.
- TOOL-011/012/015/018/019/020/021/023/024: detalles de tooling.
- DOC-003/004/005/006/010/011: docs.
- TPL-002/008/009: template nice-to-have.

## Orden de ejecución recomendado

```text
1. Batch 1  (errores de entrada + contrato)
2. Batch 2  (auth/claims)
3. Batch 3  (datos/reglas)
4. Batch 4  (arquitectura)
5. Batch 5  (cobertura)
6. Batch 6  (hooks/guardarraíles)
7. Batch 7  (template/docs)
8. B8-B12   (P2)
9. P3       (limpieza)
```

¿Por qué Batch 1 antes que arquitectura? Porque los fixes de errores/contrato son pequeños,
bien acotados y reducen el ruido de logs; además crean los tests que protegen los refactors de
los batches 3/4. La arquitectura se mueve después, con red de seguridad.

## Gates por batch

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx
dotnet format whitespace CleanWebApiTemplate.slnx --verify-no-changes
dotnet format style CleanWebApiTemplate.slnx --verify-no-changes
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
```

Para batches de template (hive aislado, dos generaciones, build, tests sin contenedores y
publish AOT del proyecto generado):

```bash
bash scripts/template-smoke.sh
```

## Métricas de éxito

| Métrica | Antes | Objetivo |
|---|---:|---:|
| Build warnings | 0 | 0 |
| AOT publish warnings | 0 | 0 |
| Tests | 211 (auditoría; 313 tras B5/B6) | ≥ 300 |
| Cobertura `src/` líneas | 82,7% (98,0% tras B5) | ≥ 90% |
| Cobertura `src/` ramas | 79,7% (95,7% tras B5) | ≥ 80% |
| Api ramas | 56,9% (95,9% tras B5) | ≥ 75% |
| Mutation score Application/Domain | No medido | ≥ 70% |
| Paquetes vulnerables | 0 | 0 |
| Secretos en historial | 0 | 0 |
| `dotnet new` smoke test | No ejecutado (verde tras B7) | Verde |
| Hooks | pre-commit | pre-commit + commit-msg + pre-push |

## Lo que no se debe hacer

- No añadir CI/CD desde esta auditoría: el repositorio lo prohíbe. Dejar los comandos listos.
- No añadir herramientas nuevas sin fijarlas (versión + checksum) y documentar su instalación.
- No silenciar IL2026/IL3050 en código para que pase `dotnet format analyzers`; el gate AOT es
  `dotnet publish`.
- No “arreglar” la cobertura con tests de constantes; usar tests de comportamiento/mutación.
- No cambiar la regla de unicidad de título sin decidir antes el contrato (global vs per-owner).
- No tocar `.agents/skills/**` ni `skills-lock.json`.

## Siguiente paso

Batches 1–7 (P1) están cerrados. Continuar con el **Batch 8 (borde de seguridad)**:
HSTS/security headers, `ForwardedHeaders`, rate limiting, límite de body, OpenBao por HTTPS y
validación de CORS (SEC-004, SEC-005, SEC-006, SEC-009, SEC-016), con tests de regresión
primero.

No se debe abrir un PR con todo el roadmap: cada batch debe ser revisable por separado.
