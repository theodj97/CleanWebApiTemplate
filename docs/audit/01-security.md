# 01 — Seguridad

## Alcance y método

Se revisaron las rutas de `src/`, la configuración, el adaptador de OpenBao, el sidecar local
de OpenAPI y el flujo de tokens local. Se combinó revisión manual con pruebas dinámicas contra
el AppHost local (`aspire start`), tokens HS256 firmados localmente y `curl`.

No se ejecutaron ZAP, gitleaks, semgrep ni trivy porque no están instalados (decisión del
usuario). Se indican los comandos exactos en `06-tooling.md` y en el roadmap.

> **Estado:** Batch 1 corrigió SEC-002 y SEC-012. Batch 2 corrigió SEC-001, SEC-003, SEC-007
> y SEC-008 (ver [`09-batch-1.md`](09-batch-1.md) y [`10-batch-2.md`](10-batch-2.md)). El
> resto sigue pendiente.

## Threat model (STRIDE)

### Activos

- Clave de firma JWT (`API_KEY`), leída de OpenBao.
- Token de OpenBao (root en local/test; token de plataforma en despliegue).
- Cadena de conexión de PostgreSQL.
- Datos de TODO: título, descripción, email del owner y del último escritor.
- Documento OpenAPI y esquema de la API.
- Clave de firma local (`local-jwt-signing-key`) y claves de `devtoken`.

### Actores

- Anónimo en Internet.
- Usuario autenticado con rol `User`, `Operator`, `Admin` o `External`.
- Token válido sin todos los claims esperados.
- Desarrollador local y sidecar Swagger.
- Operador de la plataforma que configura OpenBao/PostgreSQL.

### Fronteras de confianza

```text
Internet ──TLS──► API ──TCP/TLS──► PostgreSQL
                    │
                    ├──HTTP/HTTPS + token──► OpenBao
                    └──OIDC metadata (opcional)──► IdP
Browser ──HTTP──► sidecar Swagger ──HTTP──► API (solo local)
Test harness ──► AppHost/contenedores (solo test)
```

### STRIDE

| Amenaza | Superficie | Estado |
|---|---|---|
| **Spoofing** | JWT: issuer, audience, lifetime, firma, algoritmo | HS256 fijado, clave >= 32 bytes y modos Symmetric/Oidc separados (Batch 2). |
| **Tampering** | SQL, ordenación, mass assignment, JSON | Sin inyección SQL; parámetros y whitelist correctos (SEC-014). Sin mass assignment. |
| **Repudiation** | Escrituras de TODO | `CreatedBy`/`UpdatedBy` presentes; no hay audit log ni correlation ID. Aceptable para template. |
| **Information disclosure** | Health, OpenAPI, errores, logs, OpenBao HTTP | Health sin auth, OpenAPI en Staging, logs Trace/Debug y OpenBao HTTP (SEC-004, SEC-006, SEC-009, SEC-013). |
| **Denial of service** | Rate limiting, body size, colecciones, errores 500 | Sin rate limiting; body 30 MB; errores de entrada generan 500/logs (SEC-002, SEC-005, SEC-016). |
| **Elevation of privilege** | Políticas, ownership, claims | Sin bypass encontrado; IDOR/BOLA cubierto por tests. Claim `email` ausente -> 403 (Batch 2). |

## Hallazgos

| ID | Severidad | Hallazgo | Evidencia | Impacto |
|---|---|---|---|---|
| SEC-001 | P2 | JWT sin `ValidAlgorithms`; acepta HS512 con la misma clave | `ConfigureServices.cs:170-180`; prueba dinámica HS512 -> 201 | Defensa en profundidad débil. |
| SEC-002 | P1 | JSON malformado/campo requerido ausente -> 500 | `GlobalExceptionHandler.cs:10-34`; `TodoValidator.cs:17`; pruebas dinámicas | Errores de cliente como errores de servidor; ruido/DoS. |
| SEC-003 | P1 | Token válido sin claim `email` -> 500 | `BaseApiRouter.cs:11`; prueba dinámica | Petición válida tumba el endpoint. |
| SEC-004 | P2 | Sin HSTS, security headers ni `ForwardedHeaders` | `ConfigureApplication.cs:11-22`; cabeceras ausentes | Downgrade/clickjacking/MIME sniffing; bucles tras proxy. |
| SEC-005 | P2 | Sin rate limiting; body por defecto 30 MB | `ConfigureServices.cs`; `ConfigureApplication.cs`; 50 peticiones sin 429 | Fuerza bruta/agotamiento. |
| SEC-006 | P2 | OpenBao acepta `http` en cualquier entorno | `OpenBaoClient.cs:27-29` | Token/secretos en claro si se configura mal. |
| SEC-007 | P2 | CORS/issuer/clave JWT sin validación | `AppSettingsValidator.cs:8-25`; `ConfigureServices.cs:107-123,170-180` | Configuraciones inseguras pasan el arranque. |
| SEC-008 | P2 | Modo OIDC y simétrico mezclados; RS256 incompleto | `ConfigureServices.cs:159-180`; `AppHost.cs:140-155` | Configuración ambigua y feature rota. |
| SEC-009 | P3 | Health sin auth; OpenAPI en Staging | `ConfigureApplication.cs:35-45`; `appsettings.STAGING.json` | Divulgación de estado/esquema. |
| SEC-010 | P3 | Root token local/test; sidecar acuña tokens | `AppHost.cs:55-58`; `OpenBao.cs:50-59`; `local/openapi/.../Program.cs:101-144` | Solo local, pero amplio si se expone. |
| SEC-011 | P3 | `IsValidEmail` acepta display names; max lengths sin usar | `BaseAbstractValidator.cs:100-111`; `TodoTable.cs:23-24` | Datos sucios. |
| SEC-012 | P3 | 500 sin traceId; 404/405 body vacío | `GlobalExceptionHandler.cs:15-31`; pruebas dinámicas | Diagnóstico/contrato pobre. |
| SEC-013 | P3 | Logging Trace/Debug por defecto | `appsettings.json`; `appsettings.STAGING.json` | Ruido y posible PII. |
| SEC-014 | Info | SQL injection no encontrada | `TodoRepository.cs:262-310` | **Positivo.** |
| SEC-015 | Info | Sin paquetes vulnerables/deprecados; sin claves privadas en historial | `dotnet list package`; `git log -G` | **Positivo.** |
| SEC-016 | P3 | Sin límite de colecciones en filtros | `FilteredTodoInputValidator.cs`; `TodoRepository.cs:262-285` | Consultas grandes; menor que el body. |

## Pruebas dinámicas

Se arrancó el AppHost local y se obtuvieron tokens con `dotnet local/tools/devtoken.cs`.
No se registran tokens ni secretos en la evidencia.

| Prueba | Resultado | Lectura |
|---|---|---|
| `GET /health`, `/alive`, `/health/api`, `/health/postgresDb` sin token | 200 | Health público. |
| `GET /openapi/v1.json` | 200 | OpenAPI habilitado en Development. |
| `POST /api/Todo/` válido | 201 sin `Location` | Contrato incompleto (BUG-007). |
| `POST` con `title: null` | 500 | NRE (BUG-001). |
| `POST` sin `title` | 500 | Error de binding como 500 (SEC-002). |
| `POST` con JSON malformado | 500 | Error de binding como 500 (SEC-002). |
| `PUT` con `status: 0` | 200 y status 0 persistido | BUG-002. |
| Filtro por fragmento de título | 200 `[]` | Igualdad exacta (BUG-005). |
| Filtro sin coincidencias | 200 `[]` | Debería ser 204 según docs (BUG-006). |
| Token sin `email` | 500 | SEC-003. |
| Token HS512 con la misma clave | 201 | SEC-001. |
| Cabeceras de seguridad/HSTS | Ausentes | SEC-004. |
| 50 peticiones a `/health` y `/api/Todo/` | Sin 429 | SEC-005. |
| `GET /nope`, `GET /api/Todo/` | 404/405 con body vacío | BUG-012. |
| Body de 31 MB | 413 | Límite de Kestrel, pero 30 MB es alto. |
| Token sin `name` | 201 | `UserName` no se usa en endpoints. |
| Token sin `role` | 403 | La autorización bloquea antes del claim. |

## Lo que está bien

- **Sin inyección SQL**: todas las entradas van parametrizadas; el `ORDER BY` usa una whitelist
  (`TodoSortColumns`) y la dirección es un `bool`, no texto.
- **Ownership/IDOR**: `GetById`, `Update` y `Delete` aplican `CreatedBy` para no-admin; los
  tests de `TodoOwnershipTests` cubren los casos principales. `Filter` y `Titles` fuerzan el
  owner para no-admin.
- **Validación de JWT**: issuer, audience, lifetime y firma están activados; los tests cubren
  tokens expirados, no válidos aún, issuer/audience incorrectos, firma desconocida y `alg:none`.
- **Errores 500 genéricos**: no se filtra el stack trace ni el mensaje de la excepción al
  cliente.
- **Secretos**: no hay claves privadas ni contraseñas literales en el historial; las claves
  locales viven en user secrets. OpenBao no registra el token en los errores.
- **Supply chain**: sin vulnerabilidades ni paquetes deprecados en la fecha de la auditoría.

## Mapeo rápido a OWASP ASVS L2

| Área | Estado | Comentario |
|---|---|---|
| V1 Arquitectura | Parcial | No hay threat model ni architecture tests en el repo. |
| V2 Autenticación | Bien | JWT validado; HS256 fijado, clave >= 32 bytes y OIDC separado (Batch 2). |
| V3 Sesión | N/A | Stateless JWT; la revocación depende del IdP. |
| V4 Control de acceso | Bien | Políticas y ownership; falta manejo de claims ausentes. |
| V5 Validación | Parcial | FluentValidation; errores de binding 500 y límites de colecciones. |
| V6 Criptografía | Parcial | HS256 con longitud mínima validada (Batch 2); OpenBao HTTP sigue pendiente. |
| V7 Errores/logs | Parcial | 500 genérico; 4xx como 500; sin traceId; Trace/Debug. |
| V8 Protección de datos | Parcial | Emails en respuestas; sin cifrado en reposo (DB); TLS. |
| V9 Comunicación | Parcial | HTTPS; sin HSTS/forwarded headers; OpenBao HTTP. |
| V10 Código malicioso | Bien | Sin reflexión en runtime; AOT; paquetes limpios. |
| V11 Lógica de negocio | Parcial | Unicidad de título y `Status=0` inconsistentes. |
| V12 Ficheros | N/A | El sidecar local lee ficheros del repo; no recibe rutas de usuario. |
| V13 API | Parcial | OpenAPI; sin rate limiting; 404/405 vacíos. |
| V14 Configuración | Parcial | appsettings; secretos; falta validación y defaults seguros. |

## Recomendaciones priorizadas

1. **Corregir el manejo de errores de entrada** (SEC-002, BUG-001): `BadHttpRequestException` y
   `JsonException` deben ser 400 ProblemDetails; los validadores deben ser null-safe.
2. **Fijar el algoritmo JWT y validar la clave** (SEC-001, SEC-007): `ValidAlgorithms` y mínimo
   de 32 bytes; separar modos OIDC/simétrico.
3. **Manejar claims ausentes** (SEC-003): `FindFirst` + política que exija `email`, o 403.
4. **Añadir controles de borde** (SEC-004, SEC-005): `ForwardedHeaders`, HSTS en producción,
   security headers, rate limiting y límite de body.
5. **Endurecer OpenBao** (SEC-006): HTTPS obligatorio fuera de Development/Testing; token de
   mínimo privilegio en despliegue.
6. **Validar configuración** (SEC-007): CORS, issuer, audience, clave y OpenAPI por entorno.
7. **Automatizar** (SEC-015): gitleaks en pre-commit, auditoría de paquetes en pre-push y
   `dotnet list package --vulnerable` en el flujo de revisión.

## Comandos propuestos (no ejecutados)

```bash
# Secretos en árbol e historial
gitleaks detect --source . --log-opts=--all

# SAST
semgrep --config p/csharp --config p/security-audit .

# Dependencias e imágenes
trivy fs --scanners vuln,secret .
trivy image <imagen>

# DAST local
docker run --rm -t ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t https://localhost:5001 -r zap-report.html
```
