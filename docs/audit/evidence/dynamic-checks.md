# Evidencia — pruebas dinámicas

Fecha: 2026-10-29. AppHost local arrancado con `aspire start`; API en `https://localhost:5001`.
Los tokens se obtuvieron con `dotnet local/tools/devtoken.cs get --profile ...` y no se
registran en este documento. Las peticiones usan `curl -k` contra el certificado de desarrollo.

## Health y OpenAPI

```bash
curl -sk -o /dev/null -w '%{http_code}' https://localhost:5001/health
curl -sk -o /dev/null -w '%{http_code}' https://localhost:5001/alive
curl -sk -o /dev/null -w '%{http_code}' https://localhost:5001/health/api
curl -sk -o /dev/null -w '%{http_code}' https://localhost:5001/health/postgresDb
curl -sk -o /dev/null -w '%{http_code}' https://localhost:5001/openapi/v1.json
```

Resultado: `200` en los cinco, sin token.

## Creación válida

```bash
curl -sk -D - -X POST https://localhost:5001/api/Todo/ \
  -H "Authorization: Bearer $TOKEN_USER" \
  -H 'content-type: application/json' \
  -d '{"title":"audit-dynamic-create","description":"dynamic check"}'
```

Resultado: `201 Created`, body con `id`, `title`, `description`, `createdAt`, `status`,
`createdBy`, `updatedBy`. **No se observó cabecera `Location`.**

## Errores de entrada

| Petición | Resultado | Lectura |
|---|---|---|
| `POST {"title":null}` | `500` | NRE en `TodoValidator.ValidateTitle` |
| `POST {}` | `500` | Error de binding JSON como 500 |
| `POST {` | `500` | JSON malformado como 500 |
| `POST` con `title` de 256 caracteres | `400` | Validación correcta |
| `POST` con `description` de 1001 caracteres | `400` | Validación correcta |
| `GET /api/Todo/not-a-ulid` | `400` ProblemDetails | Validación correcta |
| `GET /nope` | `404` body vacío | Falta ProblemDetails |
| `GET /api/Todo/` | `405` body vacío | Falta ProblemDetails |

Logs del API (resumidos, sin secretos):

```text
Internal server error: Object reference not set to an instance of an object.
Internal server error: Failed to read parameter "CreateTodoRequest request" from the request body as JSON.
Internal server error: Sequence contains no matching element
```

## Status 0

```bash
curl -sk -X PUT https://localhost:5001/api/Todo/$ID \
  -H "Authorization: Bearer $TOKEN_USER" \
  -H 'content-type: application/json' \
  -d '{"status":0}'
```

Resultado: `200`, body con `"status":0`; un `GET` posterior devuelve `0`.

## Filtros

| Petición | Resultado |
|---|---|
| `POST /api/Todo/filter {"title":["audit-shared"]}` | `200 []` (el título existe, pero el filtro es igualdad exacta) |
| `POST /api/Todo/filter {"title":["definitely-no-such-title-xyz"]}` | `200 []` (la doc dice 204) |
| `POST /api/Todo/filter` con 500 IDs inválidos | `400`, respuesta pequeña por `Distinct()` |

## Unicidad de título entre owners

1. `user` crea `audit-shared-<timestamp>` -> `201`.
2. `operator` intenta crear el mismo título -> `400` con
   `Property 'Title' is already in use!`.

La documentación dice “unique per owner”; el comportamiento es global.

## Claims

| Token | Resultado |
|---|---|
| Sin claim `email`, con rol `User` | `500` (`Sequence contains no matching element`) |
| Sin claim `name`, con `email` y rol `User` | `201` (`UserName` no se usa) |
| Sin claim `role`, con `email` | `403` (la política bloquea antes) |
| HS512 firmado con la misma clave | `201` (no se fija el algoritmo) |
| HS256 válido | `201` |

## Cabeceras y CORS

```bash
curl -sk -D - -o /dev/null https://localhost:5001/health
```

Cabeceras observadas: `content-type`, `date`, `server: Kestrel`, `cache-control`, `expires`,
`pragma`. **No** hay `Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`,
`Referrer-Policy` ni `Content-Security-Policy`.

```bash
curl -sk -D - -o /dev/null -X OPTIONS https://localhost:5001/api/Todo/ \
  -H 'Origin: https://evil.example' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: authorization,content-type'
```

Resultado: `204` sin `Access-Control-Allow-Origin`; el navegador bloquearía la petición. La
configuración local tiene `AllowedOrigins: []`, así que es el comportamiento esperado.

## Rate limiting y body

```bash
for i in $(seq 1 50); do curl -sk -o /dev/null -w '%{http_code} ' https://localhost:5001/health; done
```

Resultado: 50 × `200`. Sin `429`.

Body de 31 MB:

```bash
python3 - <<'PY'
from pathlib import Path
Path('/tmp/large.json').write_bytes(b'{"title":"' + b'a' * (31 * 1024 * 1024) + b'"}')
PY
curl -sk -o /dev/null -w '%{http_code}' -X POST https://localhost:5001/api/Todo/ \
  -H "Authorization: Bearer $TOKEN_USER" \
  -H 'content-type: application/json' --data-binary @/tmp/large.json
```

Resultado: `413` (límite por defecto de Kestrel). No hay un límite explícito más bajo.

## Título con espacios

```bash
curl -sk -X POST https://localhost:5001/api/Todo/ \
  -H "Authorization: Bearer $TOKEN_USER" \
  -H 'content-type: application/json' \
  -d '{"title":"  audit-padded-title  "}'
```

Resultado: `201`, `title` almacenado como `"  audit-padded-title  "` (sin trim).

## OpenAPI

```bash
curl -sk https://localhost:5001/openapi/v1.json -o /tmp/openapi.json
```

Comprobaciones:

- `TodoResponse` properties: `createdAt`, `createdBy`, `description`, `id`, `status`, `title`,
  `updatedBy`. **No incluye `updatedAt`**, aunque `OpenApiConfiguration` lo describe.
- `CreateTodoRequest.required`: `["title"]`.
- `FilteredTodoRequest.title.description`: “Title fragments to match (contains).”
- `CreateTodoRequest.title.description`: “Short task description. Trimmed; unique per owner.”
- Paths: `/api/Todo`, `/api/Todo/filter`, `/api/Todo/titles`, `/api/Todo/{id}`.

## Smoke test del binario Native AOT

Se ejecutó el binario publicado en `bin/Release/net10.0/linux-x64/publish/` con las variables
de entorno del API local (PostgreSQL/OpenBao) y `ASPNETCORE_URLS=http://127.0.0.1:5099`:

| Prueba | Resultado |
|---|---|
| Arranque | ✅ |
| `GET /health/api` | 200 |
| `GET /openapi/v1.json` | 200 |
| `POST /api/Todo/` válido | 201 |
| `POST` con `title: null` | 500 |
| `POST` sin `title` | 500 |

Conclusión: AOT funciona en runtime; los 500 son los bugs de validación/error handling, no un
fallo de trimming.

## Limitaciones

- No se probó un proxy inverso real para `ForwardedHeaders`; se revisó el código.
- No se probó OpenBao con HTTPS; se revisó el código.
- No se probó un IdP OIDC real; el modo local es HS256.
- No se probó concurrencia real para la carrera de unicidad; se revisó el código.
