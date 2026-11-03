# Evidencia — auditoría de paquetes

Fecha: 2026-10-29.

## Vulnerabilidades

```text
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
```

Resultado: **ningún paquete vulnerable** en los 14 proyectos, con las fuentes de NuGet
configuradas en la fecha de la auditoría.

## Deprecados

```text
dotnet list CleanWebApiTemplate.slnx package --deprecated
```

Resultado: **ningún paquete deprecado**.

## Actualizaciones disponibles

| Proyecto | Paquete | Actual | Última | Tipo |
|---|---|---|---|---|
| Api | `FluentValidation` | 12.1.1 | 12.2.0 | Minor |
| Api | `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | 10.0.14 | Patch |
| Api | `Microsoft.AspNetCore.OpenApi` | 10.0.10 | 10.0.14 | Patch |
| Api | `Microsoft.OpenApi` | 2.7.5 | 3.11.0 | **Major** |
| Application | `FluentValidation` | 12.1.1 | 12.2.0 | Minor |
| Domain | `Microsoft.AspNetCore.Http` | 2.3.11 | 2.3.13 | Patch (legacy 2.x) |
| Domain | `Microsoft.AspNetCore.Http.Abstractions` | 2.3.11 | 2.3.13 | Patch (legacy 2.x) |
| Infrastructure | `Microsoft.Extensions.Configuration` | 10.0.10 | 10.0.14 | Patch |
| OpenApiExamples | `Swashbuckle.AspNetCore.SwaggerUI` | 10.2.3 | 10.3.1 | Patch |
| Tests | `xunit.v3.*` | 3.2.2 | 4.0.2 | **Major** |
| Tests | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.10 | 10.0.14 | Patch |

## Lectura

- Los patches de ASP.NET Core/Extensions son de bajo riesgo y recomendables.
- `FluentValidation` 12.2.0 es minor; revisar notas.
- `Microsoft.OpenApi` 3.x es major; puede requerir cambios en `OpenApiConfiguration`.
- `xunit.v3` 4.x es major; revisar breaking changes antes de actualizar.
- `Microsoft.AspNetCore.Http` 2.3.x en Domain es legacy y no se usa; eliminarlo es mejor que
  actualizarlo.
- No hay lock files; para reproducibilidad, valorar `RestorePackagesWithLockFile`.
- `Microsoft.Testing.Extensions.CodeCoverage` y `Microsoft.Testing.Platform` están declarados
  en `Directory.Packages.props` pero no referenciados por ningún proyecto.
