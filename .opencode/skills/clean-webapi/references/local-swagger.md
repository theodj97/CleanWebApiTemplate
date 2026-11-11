# Local Swagger and examples

The local loop proves an endpoint end to end: the API, the real database, OpenBao and a Swagger UI that runs
every example with a local token. This reference covers the loop and the examples every operation must ship.

The API produces the **OpenAPI document** at `/openapi/v1.json`. **Swagger UI** is served by the sidecar in
`local/openapi/`. The **examples catalog** is not part of the document: the sidecar merges it at request time.

## What the loop proves

| Check | Proves |
|---|---|
| The operation is in `/openapi/v1.json` | `WithName`, `WithSummary`, `Produces`, `ProducesProblem` |
| The JSON context serializes the contracts | No reflection fallback |
| `{Feature}OpenApiSchemas` describes the schema | Descriptions, formats, lengths, enums, examples |
| The catalog matches the document | `examples.json`, `bodies/`, `responses/` |
| The example runs with a profile | Seed + token + policy = documented status and body |

An operation with no runnable example is not finished.

## Start and verify

```bash
dotnet local/tools/devtoken.cs init     # once per machine
aspire start                            # background; prints the dashboard
aspire ps --format Json                 # wait for postgres, db-init, openbao, openbao-init, api, swagger
```

Open the `swagger` resource in the dashboard, pick a profile, and run each example. `aspire logs swagger`
shows catalog/document problems.

| Thing | Where |
|---|---|
| API | `https://localhost:5001` |
| Document | `https://localhost:5001/openapi/v1.json` |
| Swagger UI | the `swagger` resource (`/swagger`) |
| Merged document | the sidecar's `/openapi/v1.json` |
| Health | `/health`, `/alive`, `/health/api`, `/health/postgresDb` |

`aspire stop` removes the containers and the data.

## Catalog contract

`local/openapi/examples.json`:

```json
{
  "operations": {
    "POST /api/todos/filter": {
      "requestBody": {
        "examples": {
          "user": { "summary": "…", "description": "…", "body": "bodies/filter-user.json" }
        }
      },
      "responses": {
        "200": {
          "examples": {
            "user": { "summary": "…", "body": "responses/todos-user.json" }
          }
        }
      }
    },
    "GET /api/todos/{id}": {
      "parameters": [
        { "name": "id", "in": "path", "value": "01HWXHRB80000G000000000001" }
      ],
      "responses": {
        "200": { "examples": { "todo": { "summary": "…", "body": "responses/todo-user.json" } } }
      }
    }
  }
}
```

Rules the sidecar enforces:

- Keys are `"{METHOD} {path}"` exactly as the document names them (`POST /api/todos`, `GET /api/todos/{id}`).
- `parameters` is an **array** of objects with `name`, `in` and `value`. An object map is rejected.
- A request example has `body` XOR `value`; a response example has `body`, `value` or `content`.
- Every path must exist in the document and every parameter must exist on the operation; otherwise the
  sidecar returns 500 with the problem list.
- `body` is a path relative to `local/openapi/`.
- A response with no content (204) needs no example.
- The catalog and the body files are re-read on every request: nothing is cached, nothing is generated.

## Mandatory examples per operation

| Operation | Request | Responses |
|---|---|---|
| Create | one body with valid fields | 201 (stored row) and 409 (duplicate) |
| Get by id | the `id` path parameter with a seed id | 200 and 404 |
| Filter | one body per identity that shows scoping (user/operator/admin) | 200 per identity, and 200 `[]` |
| Titles | one paging body | 200 |
| Update | the `id` parameter and one body | 200 and 404 |
| Delete | the `id` parameter | 204 (no body) |

Bodies and responses must match the seed (`local/database/seed/*.sql`), the users
(`local/openapi/users.json`) and the devtoken profiles. A response example with a different `createdBy`,
status or id than the seed fails when the example runs.

## Adding an example

1. Add the operation key with the exact path and method.
2. Add the `parameters` array when the path has parameters.
3. Add the body file under `bodies/` and the response files under `responses/`.
4. Keep the ids, emails and statuses consistent with the seed and the profiles.
5. Check the catalog is valid JSON and the paths exist in the document (the sidecar validates on every
   request; `local/openapi/…UnitTests` covers the merger itself).
6. Run the example with the profile that matches its owner.

## Profiles

`local/openapi/users.json` drives the header picker. Selecting a profile calls the sidecar's internal
`POST /local/auth/token/{profile}` (not an OpenAPI operation) and mints an HS256 token with the same key the
AppHost seeded into OpenBao. The profiles are role-based (`user`, `operator`, `admin`, `external`, `no-role`),
not feature-based, so a new feature needs no new profile unless it introduces a role.

The sidecar proxies `/api/*` to the API, so "Try it out" is same-origin and CORS does not apply. It is local
only: never exposed, never deployed, never started by the tests.
