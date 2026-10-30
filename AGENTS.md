# Instrucciones del repositorio

## Native AOT

Este repositorio mantiene los proyectos de `src/` (en especial
`src/CleanWebApiTemplate.Api`) **100% compatibles con Native AOT**. El proyecto Api declara
`PublishAot`, `IsAotCompatible`, `EnableAotAnalyzer` y `TreatWarningsAsErrors`; no relajes esas
propiedades ni las muevas a otro proyecto para esquivar el analizador.

### Prohibido (salvo verificación explícita de compatibilidad AOT)

- Reflexión en rutas de ejecución: `System.Reflection` (`Assembly.Load`, `Type.GetType`,
  `GetMethod`, `MakeGenericType`, `Activator.CreateInstance`, etc.).
- `dynamic`.
- Serialización por reflexión: usa siempre el `JsonSerializerContext` generado
  (`AppJsonSerializerContext`), nunca el serializador por defecto de `JsonSerializerOptions`.
- Generadores OpenAPI/Swagger basados en reflexión (p. ej. Swashbuckle SwaggerGen y cualquier API
  que descargue esquemas en tiempo de ejecución).
- Silenciar avisos de AOT/trimming con `RequiresDynamicCode`, `RequiresUnreferencedCode` o
  `UnconditionalSuppressMessage`. Un aviso `IL2xxx`/`IL3xxx` se corrige en su causa, no se oculta.

### Obligatorio antes de dar un cambio por terminado

```bash
dotnet build CleanWebApiTemplate.slnx                      # 0 warnings, 0 errors
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained                 # Native AOT, 0 warnings
```

Si aparece cualquier advertencia de trimming/AOT, el cambio no está terminado. Distingue siempre
un build correcto de una publicación AOT correcta: el build no sustituye al publish.

## Hooks locales

- `npm run hooks:install` instala Husky (`dotnet tool restore && dotnet husky install`).
- `npm run verify` ejecuta los mismos gates que el hook `pre-push`: build, formato,
  analyzers, **todos** los tests (unit, component y functional; requieren Docker) y paquetes
  vulnerables.
- `npm run coverage` genera el informe y aplica los umbrales.
- gitleaks se instala solo la primera vez que se ejecuta `pre-commit`
  (`scripts/install-gitleaks.sh`, versión fijada con SHA-256).
- Saltar los hooks: `HUSKY=0 git ...` o `--no-verify`.

## Agent skills

### Issue tracker

Las incidencias viven en GitHub Issues (`theodj97/CleanWebApiTemplate`), gestionadas con la CLI
`gh`. Ver `docs/agents/issue-tracker.md`.

### Triage labels

Vocabulario por defecto de cinco roles: `needs-triage`, `needs-info`, `ready-for-agent`,
`ready-for-human`, `wontfix`. Ver `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `GLOSSARY.md` en la raíz y `docs/adr/`. Ver `docs/agents/domain.md`.
