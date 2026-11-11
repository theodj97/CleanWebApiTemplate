# Local OpenAPI examples

The Swagger sidecar (`local/openapi/`) merges this catalog into the API document. Keys are OpenAPI operation
paths; paths without trailing slash match the generated document (`POST /api/Project`, while the route is
mapped as `/`).

## `examples.json`

```json
{
  "operations": {
    "POST /api/Project/filter": {
      "requestBody": {
        "examples": {
          "user": {
            "summary": "Projects of the local user",
            "description": "Matches Release 1.0 and Write the runbook. Authorize with the user profile.",
            "body": "bodies/filter-projects-user.json"
          },
          "admin": {
            "summary": "Projects of several users",
            "description": "Matches every seeded project. Authorize with the admin profile.",
            "body": "bodies/filter-projects-admin.json"
          }
        }
      },
      "responses": {
        "200": {
          "examples": {
            "user": {
              "summary": "Two projects owned by the user",
              "body": "responses/projects-user.json"
            },
            "admin": {
              "summary": "All seeded projects",
              "body": "responses/projects.json"
            }
          }
        }
      }
    },
    "POST /api/Project": {
      "requestBody": {
        "examples": {
          "new-project": {
            "summary": "Create a project",
            "description": "Returns 201 once. Edit the name to create another one, or restart Aspire to rebuild the seed.",
            "body": "bodies/create-project.json"
          }
        }
      },
      "responses": {
        "201": {
          "examples": {
            "created": {
              "summary": "The stored project",
              "body": "responses/project-created.json"
            }
          }
        }
      }
    },
    "GET /api/Project/{id}": {
      "parameters": {
        "id": {
          "examples": {
            "seeded": {
              "summary": "A project owned by the local user",
              "value": "01HWXHRB80000G000000000001"
            }
          }
        }
      },
      "responses": {
        "200": {
          "examples": {
            "seeded": {
              "summary": "The seeded project",
              "body": "responses/project-user.json"
            }
          }
        }
      }
    }
  }
}
```

## `bodies/filter-projects-user.json`

```json
{
  "sortProperties": [
    { "key": "createdAt", "value": true }
  ],
  "pageNumber": 1,
  "pageSize": 10
}
```

## `bodies/filter-projects-admin.json`

```json
{
  "createdBys": [
    "local.user@example.com",
    "local.operator@example.com",
    "local.admin@example.com"
  ],
  "sortProperties": [
    { "key": "createdAt", "value": true }
  ]
}
```

## `bodies/create-project.json`

```json
{
  "name": "Created from the local Swagger examples",
  "description": "Example request against the Aspire seed."
}
```

## `responses/project-user.json`

```json
{
  "id": "01HWXHRB80000G000000000001",
  "name": "Release 1.0",
  "description": "Ship the first public release.",
  "createdAt": "2026-10-20T09:00:00Z",
  "status": 2,
  "createdBy": "local.user@example.com",
  "updatedBy": "local.user@example.com"
}
```

## `responses/projects-user.json`

```json
[
  {
    "id": "01HWXHRB80000G000000000001",
    "name": "Release 1.0",
    "description": "Ship the first public release.",
    "createdAt": "2026-10-20T09:00:00Z",
    "status": 2,
    "createdBy": "local.user@example.com",
    "updatedBy": "local.user@example.com"
  },
  {
    "id": "01HWXHRB80000G000000000002",
    "name": "Write the runbook",
    "description": "Document the local environment and the deployment steps.",
    "createdAt": "2026-10-21T10:15:00Z",
    "status": 1,
    "createdBy": "local.user@example.com",
    "updatedBy": "local.user@example.com"
  }
]
```

## `responses/projects.json`

```json
[
  {
    "id": "01HWXHRB80000G000000000001",
    "name": "Release 1.0",
    "description": "Ship the first public release.",
    "createdAt": "2026-10-20T09:00:00Z",
    "status": 2,
    "createdBy": "local.user@example.com",
    "updatedBy": "local.user@example.com"
  },
  {
    "id": "01HWXHRB80000G000000000002",
    "name": "Write the runbook",
    "description": "Document the local environment and the deployment steps.",
    "createdAt": "2026-10-21T10:15:00Z",
    "status": 1,
    "createdBy": "local.user@example.com",
    "updatedBy": "local.user@example.com"
  },
  {
    "id": "01HWXHRB80000G000000000003",
    "name": "Migrate the database",
    "description": "Apply the new schema to every environment.",
    "createdAt": "2026-10-18T08:00:00Z",
    "status": 3,
    "createdBy": "local.operator@example.com",
    "updatedBy": "local.operator@example.com"
  },
  {
    "id": "01HWXHRB80000G000000000004",
    "name": "Quarterly audit",
    "description": "Archive the findings and the remediation plan.",
    "createdAt": "2026-10-01T07:45:00Z",
    "status": 4,
    "createdBy": "local.admin@example.com",
    "updatedBy": "local.admin@example.com"
  }
]
```

## `responses/project-created.json`

```json
{
  "id": "01HWXHRB80000G000000000005",
  "name": "Created from the local Swagger examples",
  "description": "Example request against the Aspire seed.",
  "createdAt": "2026-10-28T09:30:00Z",
  "status": 1,
  "createdBy": "local.user@example.com",
  "updatedBy": "local.user@example.com"
}
```

The ids in `responses/project-user.json` and `responses/projects.json` match the seed rows in
`database/seed/0002__projects.sql` (their `bytea` values decode to these 26-character ULIDs). The id in
`responses/project-created.json` is illustrative: a create generates a new ULID, so the response id changes on
every call. To make the "GET by id" example callable against the seed, use the id of a seeded row returned by
the filter operation.
