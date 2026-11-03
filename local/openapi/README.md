# Local OpenAPI examples and login

`local/openapi/` is the local-only layer that populates Swagger with reusable request and response data and a profile
picker. It is **not** part of the API project and it is **not** published: `CleanWebApiTemplate.OpenApiExamples` is a
small sidecar that Aspire starts next to the API in Local mode.

## How it works

1. The sidecar fetches `GET https://localhost:5001/openapi/v1.json` from the API.
2. It applies `examples.json` to the document: request body examples, parameter examples and response examples.
3. It ensures the `Bearer` security scheme is present. The custom UI adds a header profile picker with the users in
   `users.json`; selecting one calls the sidecar's internal `POST /local/auth/token/{profile}` and applies the token
   with `ui.preauthorizeApiKey`. No login operation is added to the document.
4. It serves its own Swagger UI at `/swagger` and proxies `/api/*` to the API, so the browser only talks to one
   origin (no CORS).
5. The document and catalog are read on every request; the users are read at startup. Nothing is persisted: the seed
   is rebuilt by `db-init` on every `aspire start`.

Open the `swagger` resource in the Aspire dashboard; it is the only Swagger UI. The API only exposes
`/openapi/v1.json`.

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
      },
      "responses": {
        "200": {
          "examples": {
            "user": {
              "summary": "TODO 1, owned by the user",
              "body": "responses/todos-user.json"
            }
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
- `parameters` is an array; each entry needs `name` and `value`. `in` is optional unless the same name exists in more
  than one location.
- `requestBody.examples` and `responses.<status>.examples` accept the same shape: `summary`, `description` and one of
  `body` or `value`.
- The path template in the key uses `{id}`. The sidecar normalizes route constraints (`{id:ulid}`) and trailing
  slashes before matching.
- The default content type is `application/json`; use `contentType` inside `requestBody` or a response to pick another
  one.
- Body files are plain JSON; they are not templates. Use literal values.
- Responses without an explicit example get a generic `ProblemDetails` example (title, status and detail) instead of
  Swagger UI's schema defaults. The catalog can override it like any other response.

The current catalog covers the Todo endpoints and the fixed ids of `local/database/seed/0001__todos.sql`. If the seed
changes, update the ids in `examples.json` and `responses/`.

## Users and login

`users.json` defines the profile picker users and the JWT they receive:

```json
{
  "users": {
    "user": {
      "name": "local.user",
      "email": "local.user@example.com",
      "roles": ["User"],
      "subject": "0b7c2f4e-0000-4000-8000-000000000001"
    }
  }
}
```

- The five profiles match `local/tools/devtoken.json`: `user`, `operator` and `admin` own the seeded rows; `external`
  and `no-role` exercise 403.
- `email` is required and must match the seed's `CreatedBy` for the ownership checks. `name`, `roles` and `subject`
  are optional; `name`/`subject` default to the profile key.
- Selecting a profile in the header mints the same HS256 token as `devtoken get`: same issuer, audience, role claim,
  `sub`, `name`, `role` array, `email` and 60-minute lifetime.
- The sidecar receives the signing key (the same text seeded into OpenBao as `API_KEY`) and the issuer/audience/role
  claim from the AppHost. It is HS256-only; local development uses HS256. Deployed environments use OIDC
  (`Authentication:Mode=Oidc`) for asymmetric algorithms.
- The token lives in the page, not in localStorage: no `persistAuthorization`. After a refresh, select the profile
  again; "New token" mints a new one. The `devtoken` CLI remains for scripts and `curl`.
- The UI follows the system colour scheme, keeps a light/dark preference in `localStorage` and never persists the
  token.
- The internal `POST /local/auth/token/{profile}` route is not added to the OpenAPI document; the panel only shows the
  API operations and the profile picker.

## Adding an example

1. Add the body file under `bodies/` or `responses/` (or use `value` inline).
2. Add the operation to `examples.json` with the key `METHOD /path`, under `requestBody.examples` or
   `responses.<status>.examples`.
3. Refresh the Swagger UI. The sidecar reads the catalog on every request; no restart is needed.

If an operation, parameter or response does not exist in the API document, the document endpoint returns `500` with
the list of problems, and the same list is written to the `swagger` resource logs.

## Tests

The local-only tests live next to the sidecar:

```bash
dotnet test --project local/openapi/CleanWebApiTemplate.OpenApiExamples.UnitTests/CleanWebApiTemplate.OpenApiExamples.UnitTests.csproj
```

They cover the catalog merger, `users.json`, the JWT claims/signature and the custom index; no containers.

## Limitations

- Swagger executes requests; it does not run assertions. Use a test for that.
- Only `/api/*` is proxied. The API's health and OpenAPI endpoints are not part of the examples UI.
- No templating: examples are static files.
- Only the happy-path response examples are catalogued; error responses get a generic `ProblemDetails` example.
- Local only: the sidecar is not started by the Component/Functional tests and is not published.

## Troubleshooting

- **`Could not read the OpenAPI document from ...`**: the API is not up yet, or `OpenApi:Enabled` is false. Wait for
  the API, then refresh.
- **TLS error**: the sidecar uses the development certificate. Run `dotnet dev-certs https --trust` and start Aspire
  from a shell with `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"` (Linux).
- **`500` with problems**: the catalog does not match the current document; read the list in the `swagger` resource
  logs.
- **401 after selecting a profile**: the token expired or the key rotated; select the profile again or press
  "New token".
- **`Unknown profile`**: `users.json` changed while the sidecar was running; restart the `swagger` resource or Aspire.
