# 07 — Template y documentación

## `template.json` actual

```json
{
  "author": "Theo Delvas Játiva",
  "repository": { "url": "https://github.com/theodj97/CleanWebApiTemplate", "type": "GitHub" },
  "classifications": [],
  "identity": "CleanWebApiTemplate",
  "name": "",
  "description": "",
  "shortName": "CleanWebApiTemplate",
  "sourceName": "CleanWebApiTemplate",
  "tags": { "language": "C#", "type": "project" },
  "preferNameDirectory": true,
  "sources": [
    {
      "modifiers": [
        {
          "exclude": [
            ".git/**",
            ".gitignore",
            ".husky/_/**",
            "README.md",
            "node_modules/**",
            "**/[Bb]in/**",
            "**/[Oo]bj/**"
          ]
        }
      ]
    }
  ]
}
```

## Hallazgos

| ID | Severidad | Hallazgo | Evidencia | Impacto |
|---|---|---|---|---|
| TPL-001 | P1 | `name`, `description` y `classifications` vacíos; `dotnet new install` falla | `template.json:8-14`; `dotnet new install` -> `[Error][MV002] Missing 'name'` | El template **no se puede instalar**. |
| TPL-002 | P3 | `shortName` en PascalCase | `template.json:14` | Convención; los short names suelen ser minúsculas. |
| TPL-003 | P1 | Excluye `.gitignore` y `README.md` | `template.json:27-29` | El proyecto generado no tiene `.gitignore` ni README. |
| TPL-004 | P1 | No excluye `coverage-report/`, `.agents/`, `skills-lock.json`, `.task-skill/`, `docs/audit/` | `template.json:25-33`; 4,0 MB + 3,9 MB + 12 KB | El paquete del template incluye artefactos locales, skills y la auditoría. |
| TPL-005 | P1 | `UserSecretsId` fijos en Api y AppHost | `Api.csproj:8`; `AppHost.csproj:9` | Dos proyectos generados comparten user secrets. |
| TPL-006 | P1 | Sin `symbols` ni `postActions` | `template.json` | No se parametriza nada ni se restauran tools/hooks. |
| TPL-007 | P1 | Sin smoke test de `dotnet new` | No ejecutado | No se sabe si el template genera un proyecto compilable. |
| TPL-008 | P2 | Sin flags de features opcionales | `template.json` | Template rígido (OpenBao, sample Todo, auth). |
| TPL-009 | P2 | Proyecto generado sin LICENSE/CONTRIBUTING | Raíz; `template.json` | Template incompleto. |

## Empaquetado: qué incluiría hoy

El motor de templates empaqueta el contenido del directorio fuente, no solo lo trackeado por
git. Con las exclusiones actuales, el paquete incluiría:

| Ruta | Tamaño | ¿Debería incluirse? |
|---|---:|---|
| `src/`, `tests/`, `local/` | — | Sí |
| `.config/`, `.husky/`, `.vscode/`, `.editorconfig`, `.gitattributes` | — | Sí |
| `AGENTS.md`, `local/local-development.md`, `tests/README.md`, `local/openapi/README.md` | — | Sí (revisar) |
| `coverage-report/` | 4,0 MB | **No** |
| `.agents/` | 3,9 MB | **No** (skills locales) |
| `skills-lock.json` | 12 KB | **No** |
| `.task-skill/` | — | **No** |
| `docs/audit/` | — | **No** (auditoría del repo, no del template) |
| `.gitignore` | — | **Sí** (hoy excluido) |
| `README.md` | 0 bytes | **Sí**, pero con contenido de template (hoy excluido) |

**Acción:** añadir a `exclude`:

```text
"coverage-report/**",
".agents/**",
"skills-lock.json",
".task-skill/**",
"docs/audit/**",
```

y quitar `.gitignore` y `README.md` de la lista (o usar un README específico de template).

## GUIDs

`UserSecretsId` es fijo:

```xml
<UserSecretsId>a4490548-cd9d-4321-82ca-b9353e1834c4</UserSecretsId>  <!-- Api -->
<UserSecretsId>6f4b1d0e-2c3a-4e5f-9a7b-1c2d3e4f5a6b</UserSecretsId>  <!-- AppHost -->
```

Dos proyectos generados desde el template compartirían los mismos user secrets. En un template
hay que generar un GUID por proyecto. El motor de templates soporta símbolos `guid`:

```json
"symbols": {
  "ApiUserSecretsId": {
    "type": "generated",
    "generator": "guid",
    "parameters": { "format": "D" },
    "replaces": "a4490548-cd9d-4321-82ca-b9353e1834c4"
  },
  "AppHostUserSecretsId": {
    "type": "generated",
    "generator": "guid",
    "parameters": { "format": "D" },
    "replaces": "6f4b1d0e-2c3a-4e5f-9a7b-1c2d3e4f5a6b"
  }
}
```

Alternativa: eliminar `UserSecretsId` del template y añadirlo con un post-action
`dotnet user-secrets init`.

## Metadatos

Propuesta:

```json
{
  "name": "Clean Web API Template",
  "description": "ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT, JWT, PostgreSQL, OpenBao and Aspire.",
  "classifications": ["Web", "Web API", "Clean Architecture", "Aspire"],
  "shortName": "clean-webapi",
  "tags": {
    "language": "C#",
    "type": "project",
    "framework": "net10.0"
  }
}
```

`shortName` puede ser un array para alias:

```json
"shortName": ["clean-webapi", "cleanwebapi"]
```

## Post-actions

```json
"postActions": [
  {
    "description": "Restore NuGet packages",
    "manualInstructions": [{ "text": "Run 'dotnet restore'." }],
    "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
    "continueOnError": true
  },
  {
    "description": "Restore local tools and install Husky hooks",
    "manualInstructions": [{ "text": "Run 'dotnet tool restore' and 'dotnet husky install'." }],
    "actionId": "3A7C4B69-1B5C-4C3A-9B0A-3E9F6B7C8D9E",
    "continueOnError": true
  }
]
```

Los `actionId` deben ser GUIDs válidos; el ejemplo es ilustrativo. Para `dotnet restore` se
puede usar el action id estándar de `dotnet new` (`210D431B-A78B-4D2F-B762-4ED3E3EA9025`).
Para `dotnet tool restore`/`husky install` no hay un action estándar; se puede usar un
`postAction` con `manualInstructions` o un script.

## Smoke test del template

Se intentó instalar la plantilla en un `HOME`/`DOTNET_CLI_HOME` temporal (sin tocar el entorno
global) y **falló antes de generar nada**:

```text
dotnet new install /home/theo/repos/theo/CleanWebApiTemplate
Error: The template '<no name>' (CleanWebApiTemplate) has the following validation errors:
   [Error][MV002] Missing 'name'.
No templates were found in the package /home/theo/repos/theo/CleanWebApiTemplate.
```

Por tanto no se pudo ejecutar el smoke test completo. Una vez corregido `name`, el
procedimiento recomendado es:

```bash
# En un directorio temporal, con el repo como fuente
dotnet new install /home/theo/repos/theo/CleanWebApiTemplate
dotnet new clean-webapi -n Sample -o /tmp/opencode/template-smoke/Sample
cd /tmp/opencode/template-smoke/Sample
dotnet restore
dotnet tool restore
dotnet husky install
dotnet build
dotnet test --project tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
```

Criterios de aceptación:

- No queda la cadena `CleanWebApiTemplate` en nombres de archivo, namespaces o contenido.
- `UserSecretsId` son GUIDs distintos.
- Existe `.gitignore`.
- Existe `README.md` con quickstart.
- No existe `coverage-report/`, `.agents/`, `skills-lock.json`.
- `dotnet build` y `dotnet publish` AOT pasan sin warnings.
- Los tests unitarios pasan.
- `dotnet husky install` deja hooks funcionales.
- `aspire run` arranca (manual, requiere Docker/certificado).

## Matriz README vs realidad

| Documento | Qué afirma | Realidad | Acción |
|---|---|---|---|
| `README.md` | — | Vacío (0 bytes) | Reescribir: quickstart, arquitectura, configuración, secretos, tests, template, troubleshooting. |
| `tests/README.md` | Comandos MTP, Docker, matriz de identidades, troubleshooting | Verificado: comandos y matriz correctos; los proyectos pueden correr en paralelo | Mantener; añadir nota de cobertura. |
| `local/openapi/README.md` | Sidecar, catálogo, usuarios, tokens HS256 | Verificado; coherente con el código | Mantener; revisar si se añade RS256. |
| `local/local-development.md` | Topología Aspire, puertos, secretos, health, troubleshooting | Mayormente correcto; drift en Docker 29.8.2 vs 29.1.3, SDK 10.0.113 vs `global.json` 10.0.100; `/health` no reporta la dependencia fallida; `API_KEY` corta no se valida | Corregir los cuatro puntos. |
| `AGENTS.md` | Referencias a `docs/agents/issue-tracker.md`, `triage-labels.md`, `domain.md`, `GLOSSARY.md`, `docs/adr/` | No existen | Crear docs o corregir referencias. |
| `local/tools/devtoken.json` | Perfiles, issuer, audiencia | Correcto; sin claves | Mantener. |
| `package.json` | `homepage` apunta al README | README vacío | Corregir al reescribir. |

### Drift concreto en `local/local-development.md`

1. **Docker**: dice 29.8.2; instalado 29.1.3.
2. **SDK**: dice 10.0.113 (`global.json`); `global.json` dice 10.0.100 con `latestFeature`.
3. **Health**: dice “the health check reports the failing dependency”; el writer por defecto
   solo devuelve `Healthy`/`Unhealthy` agregado. Si se quiere detalle, añadir un
   `ResponseWriter` que liste checks, o corregir la frase.
4. **API_KEY corta**: dice que una clave demasiado corta causa 500; el código no valida la
   longitud. O se valida (recomendado) o se corrige la doc.
5. **OpenAPI/Title**: la doc de la API dice “contains”, “trimmed” y “unique per owner”; el
   runtime hace igualdad exacta, no recorta y usa unicidad global. Corregir código o docs.

### Drift en `AGENTS.md`

`AGENTS.md` referencia:

- `docs/agents/issue-tracker.md`
- `docs/agents/triage-labels.md`
- `docs/agents/domain.md`
- `GLOSSARY.md`
- `docs/adr/`

Ninguno existe en el repo. Opciones:

1. Crear `docs/agents/` con los tres documentos y `GLOSSARY.md`/`docs/adr/`.
2. Eliminar las referencias y dejar solo las instrucciones de AOT.
3. Mover esas instrucciones a `CONTRIBUTING.md`.

Para un template, la opción 1 es la más coherente si se quiere conservar el flujo de agentes;
la 2 es la más simple si el template no va a usar ese flujo.

## README raíz propuesto (esquema)

```markdown
# Clean Web API Template

ASP.NET Core 10 Minimal API with Clean Architecture, Native AOT, JWT, PostgreSQL,
OpenBao and Aspire.

## Quickstart
- Requirements: .NET 10 SDK, Docker, Aspire CLI, dev cert.
- `dotnet tool restore`
- `dotnet husky install`
- `dotnet local/tools/devtoken.cs init`
- `aspire start`

## Architecture
- src/Api, Application, Domain, Infrastructure, ServiceDefaults.
- Dependency rule.
- AOT constraints.

## Configuration and secrets
- appsettings, OpenBao, user secrets.
- Environment variables.
- JWT modes.

## API
- Endpoints, auth matrix, status codes, OpenAPI.

## Testing
- Unit/component/functional.
- Docker, MTP, coverage.

## Template usage
- `dotnet new install .`
- `dotnet new clean-webapi -n MyApi`
- Post-actions.

## Troubleshooting
- Common errors.

## License
```

## Bruno

No se encontraron referencias a Bruno ni archivos `.bru`/`bruno.json` en el repositorio
(`grep -Ri bruno`, `find -iname '*bruno*'`). No hay nada que eliminar por ese lado; al
reescribir los READMEs, simplemente no reintroducir Bruno como herramienta de pruebas.

## Conclusión

El template tiene una base técnica muy buena, pero **no está listo para `dotnet new`**:
metadatos vacíos, GUIDs compartidos, README excluido, artefactos locales incluidos y ausencia
de smoke test. La documentación local es detallada, pero el README raíz vacío y las
referencias rotas de `AGENTS.md` rompen el onboarding.
