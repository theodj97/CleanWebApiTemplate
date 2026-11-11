---
description: Add one endpoint to an existing feature
agent: build
---

Add the endpoint `$ARGUMENTS` to an existing feature.

1. Load the `clean-webapi` skill (mode **Endpoint**) and read `references/api.md` and
   `references/feature-slice.md` § Adding one endpoint.
2. Identify the feature from the route; do not create a new layer folder for a single operation.
3. Add: request (if it has a body), response (if it returns a shape), input, validator, handler, port method
   (only if a new query is needed), endpoint method, route metadata, `AddUseCase` registration, JSON context
   entry, OpenAPI descriptor, local Swagger example and tests.
4. Run the gates: build, AOT publish, the affected tests and coverage.
5. Report the real results.

Keep the contract consistent: 404 for a missing or invisible row, `200 []` for an empty list, 204 for a
successful delete, 409 for a duplicate, and ProblemDetails for every error.
