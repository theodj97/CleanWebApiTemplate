# 10 — Remediación Batch 2

Fecha: 2026-10-30
Estado: **implementado y verificado**

## Alcance

Batch 2 del roadmap: autenticación y claims.

| ID | Estado | Cambio |
|---|---|---|
| SEC-001 | ✅ | HS256 fijado con `ValidAlgorithms` en modo simétrico. |
| SEC-003 | ✅ | Token sin claim `email` -> 403, nunca 500. |
| SEC-007 | ✅ | `API_KEY` >= 32 bytes; issuer HTTPS/URN; modos separados. |
| SEC-008 | ✅ | Modos `Symmetric`/`Oidc` explícitos; RS256 local eliminado; OIDC para producción. |

## Diseño de autenticación

### Modos

`Authentication:Mode` acepta:

- `Symmetric` (alias `Local`): clave HS256 en `API_KEY`.
- `Oidc`: autoridad OIDC en `Authentication:Authority`.

Si `Authentication:Mode` no está definido, se infiere:

- sin `Authentication:Authority` -> `Symmetric`;
- con `Authentication:Authority` -> `Oidc`.

La inferencia mantiene compatibilidad con el AppHost local y los tests, pero el modo se puede
hacer explícito en producción.

### Modo Symmetric

- `API_KEY` obligatoria.
- Debe tener **>= 32 bytes UTF-8**; si no, el arranque falla con un mensaje claro.
- `IssuerSigningKey` se construye con la clave.
- `ValidAlgorithms = [HS256]`: un token HS512/HS384 con la misma clave se rechaza con 401.
- `Authentication:Authority` debe estar vacío; mezclar autoridad y clave simétrica falla.

### Modo Oidc

- `Authentication:Authority` obligatoria.
- Debe ser una URI absoluta **HTTPS**.
- Las claves de firma vienen del metadata del proveedor; no se usa `IssuerSigningKey`.
- Un `API_KEY` presente se ignora deliberadamente para no mezclar modos.
- `RequireHttpsMetadata = true` explícito.

### Issuers

`AppSettingsValidator` valida cada `ValidIssuers`:

- URI absoluta HTTPS, o URN (los tests usan `urn:CleanWebApiTemplate:tests`);
- sin entradas vacías.

### Claim email

Las cuatro políticas (`Admin`, `Operator`, `User`, `External`) exigen un claim `email`
(`ClaimTypes.Email` o `"email"`). Un token válido con rol `User` pero sin email recibe **403**
antes de entrar al handler. `BaseApiRouter.UserEmail` ya no usa `First()` y lanza un error
descriptivo si se llamara fuera de una política válida.

### RS256 local

Se eliminó el soporte local de RS256, que estaba a medias:

- `AppHost` solo acepta `LocalAuth:Algorithm=HS256`; con otro valor falla al arrancar con un
  mensaje que remite a OIDC.
- `devtoken` solo genera HS256.
- `TestTokenIssuer` solo genera HS256.
- `AppHostContract` ya no expone `PublicKeyPem` ni `local-jwt-public-key`.
- La documentación de `local/openapi` explica que el sidecar es HS256-only y que OIDC es el
  camino asimétrico en despliegue.

Esto elimina una feature rota y deja dos caminos honestos: HS256 local/tests y OIDC producción.

## Tests añadidos

### Unitarios (134 en total, +13)

- `AuthenticationSettingsTests`:
  - inferencia Symmetric/Oidc;
  - modo Symmetric con autoridad -> falla;
  - clave corta/ausente -> falla;
  - autoridad OIDC HTTP/inválida -> falla;
  - modo OIDC sin autoridad -> falla;
  - modo inválido -> falla;
  - API_KEY ignorada en OIDC.
- `AppSettingsValidatorTests`:
  - issuer HTTP -> falla;
  - issuer URN -> pasa;
  - issuer vacío -> falla.

### Componente (64 en total, +2)

- `AuthenticationTests.CreateTodo_WithInvalidToken_ReturnsUnauthorized` incluye
  `InvalidToken.WrongAlgorithm` (HS512) -> 401.
- `AuthenticationTests.CreateTodo_AuthenticatedWithoutEmail_ReturnsForbidden` -> 403.

### Functional / OpenAPI examples

Sin cambios; siguen pasando (11 + 33).

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build CleanWebApiTemplate.slnx` | ✅ 0 warnings, 0 errors |
| `dotnet publish ... -c Release -r linux-x64 --self-contained` | ✅ 0 warnings AOT |
| `dotnet test --solution CleanWebApiTemplate.slnx` | ✅ 242 tests, 0 failed |
| `dotnet format whitespace/style` | ✅ |
| `dotnet format analyzers` (excluyendo IL2026/IL3050) | ✅ |
| Cobertura `src/` tras Batch 2 | 84,0% líneas / 81,3% ramas |
| Api | 72,0% líneas / 65,3% ramas |
| Smoke test AOT | ✅ 201 válido, 403 sin email, 401 HS512 |

### Pruebas dinámicas

| Prueba | Resultado |
|---|---|
| Token HS256 válido + `POST /api/Todo/` | 201 |
| Token HS256 sin claim `email` | **403** ProblemDetails |
| Token HS512 firmado con la misma clave | **401** ProblemDetails |
| Token sin rol | 403 (sin cambios) |
| Token con rol pero sin email | 403 (nuevo) |
| Binario AOT con las mismas pruebas | 201 / 403 / 401 |

## Configuración de ejemplo

### Local / tests (Symmetric)

```json
{
  "Authentication": { "Mode": "Symmetric" },
  "API_KEY": "<32+ bytes, seeded by OpenBao locally>",
  "ValidIssuers": ["https://localhost"],
  "Audience": "https://localhost:5001"
}
```

`Authentication:Mode` puede omitirse; sin `Authentication:Authority` se infiere Symmetric.

### Producción con OIDC

```json
{
  "Authentication": {
    "Mode": "Oidc",
    "Authority": "https://login.example.com"
  },
  "ValidIssuers": ["https://login.example.com"],
  "Audience": "https://api.example.com"
}
```

No se necesita `API_KEY` en OIDC. Si está presente, se ignora.

## Pendiente

- **Batch 3**: unicidad de título, índice único, filtro contains, trim, cultura de fechas,
  concurrencia.
- **Batch 4**: arquitectura (Application -> Infrastructure, Domain -> ASP.NET, Result).
- **Batch 5/6/7**: cobertura de OpenAPI, mutation/hooks y template/readmes.

## Archivos modificados

```text
src/CleanWebApiTemplate.Api/Configuration/AuthenticationSettings.cs          (nuevo)
src/CleanWebApiTemplate.Api/Configuration/AppSettingsValidator.cs
src/CleanWebApiTemplate.Api/ConfigureServices.cs
src/CleanWebApiTemplate.Api/Common/BaseApiRouter.cs
src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj                   (InternalsVisibleTo)
src/CleanWebApiTemplate.Domain/Configuration/Constants.cs
local/aspire/CleanWebApiTemplate.AppHost/AppHost.cs
local/aspire/CleanWebApiTemplate.AppHost/AppHostContract.cs
local/aspire/CleanWebApiTemplate.AppHost/OpenBao.cs
local/tools/devtoken.cs
local/openapi/README.md
tests/CleanWebApiTemplate.Api.UnitTests/Api/Configuration/AuthenticationSettingsTests.cs (nuevo)
tests/CleanWebApiTemplate.Api.UnitTests/Api/Configuration/AppSettingsValidatorTests.cs
tests/CleanWebApiTemplate.Tests.Shared/Auth/TestTokenIssuer.cs
tests/CleanWebApiTemplate.Tests.Shared/Auth/TestUser.cs
tests/CleanWebApiTemplate.Tests.Shared/Auth/TestUsers.cs
tests/CleanWebApiTemplate.Tests.Shared/Auth/TestAuthSettings.cs
tests/CleanWebApiTemplate.Api.ComponentTests/Todo/AuthenticationTests.cs
```
