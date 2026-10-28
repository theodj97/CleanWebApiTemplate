# Local OpenAPI examples

`local/openapi/` is the local-only layer that populates Swagger with reusable request data. It is
**not** part of the API project and it is **not** published: `CleanWebApiTemplate.OpenApiExamples`
is a small sidecar that Aspire starts next to the API in Local mode.

## How it works

1. The sidecar fetches `GET https://localhost:5001/openapi/v1.json` from the API.
2. It applies `examples.json` to the document: request body examples and parameter examples.
3. It injects the `Bearer` security scheme. The token is entered by hand; there is no login endpoint.
4. It serves its own Swagger UI at `/swagger` and proxies `/api/*` to the API, so the browser only
   talks to one origin (no CORS).
5. Everything is read on every request: nothing is generated, cached or persisted. The seed is
   rebuilt by `db-init` on every `aspire start`.

Open the `swagger` resource in the Aspire dashboard. The API's own `/swagger` remains the plain UI;
the sidecar is the one with the examples.

## Catalog

`examples.json` maps `METHOD /path` to the examples to inject:

```json
{
  "operations": {
    "POST /api/Todo/filter": {
      "requestBody": {
        "examples": {
          "user": {
            "summary": "TODOs of the local user",
            "body": "bodies/filter-user.json"
          }
        }
      }
    },
    "GET /api/Todo/{id}": {
      "parameters": [
        { "name": "id", "in": "path", "value": "01HWXHRB80000G000000000001" }
      ]
    }
  }
}
```

- `body` is a file relative to this directory; `value` is an inline JSON value. Use one or the other.
- `parameters` is an array; each entry needs `name` and `value`. `in` is optional unless the same
  name exists in more than one location.
- The path template in the key uses `{id}`. The sidecar normalizes route constraints (`{id:ulid}`)
  and trailing slashes before matching.
- The default content type is `application/json`; use `requestBody.contentType` for another one.
- Body files are plain JSON; they are not templates. Use literal values.

The current catalog covers the Todo endpoints and the fixed ids of
`local/database/seed/0001__todos.sql`. If the seed changes, update the ids here.

## Authentication

The sidecar adds the `Bearer` scheme; the API document does not define one. Paste a token from:

```bash
dotnet local/tools/devtoken.cs get --profile user
```

The token is not persisted: paste it again after a refresh or restart. The profile must match the
endpoint policy (see `local/local-development.md` § Authentication).

## Adding an example

1. Add the body file under `bodies/` (or use `value` inline).
2. Add the operation to `examples.json` with the key `METHOD /path`.
3. Refresh the Swagger UI. The sidecar reads the catalog on every request; no restart is needed.

If an operation or parameter does not exist in the API document, the document endpoint returns `500`
with the list of problems, and the same list is written to the `swagger` resource logs.

## Limitations

- Swagger executes requests; it does not run assertions. Use Bruno or a test for that.
- Only `/api/*` is proxied. The API's health and OpenAPI endpoints are not part of the examples UI.
- No token generation, no response examples and no templating.
- Local only: the sidecar is not started by the Component/Functional tests and is not published.

## Troubleshooting

- **`Could not read the OpenAPI document from ...`**: the API is not up yet, or
  `OpenApi:Enabled` is false. Wait for the API, then refresh.
- **TLS error**: the sidecar uses the development certificate. Run
  `dotnet dev-certs https --trust` and start Aspire from a shell with
  `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"` (Linux).
- **`500` with problems**: the catalog does not match the current document; read the list in the
  `swagger` resource logs.
