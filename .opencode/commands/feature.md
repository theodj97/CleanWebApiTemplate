---
description: Add a complete feature slice
agent: build
---

Add the feature `$ARGUMENTS` to this repository.

1. Load the `clean-webapi` skill (mode **Feature**) and read `references/feature-slice.md`.
2. Read the closest existing feature (`Todo` or the `Project` example) before writing code.
3. Present the plan: routes and status codes, one file per layer, the schema and seed, the module
   registration, the JSON context and OpenAPI entries, the tests and the local Swagger examples.
   Wait for confirmation if the feature adds a table, a secret, a policy or changes the HTTP contract.
4. Implement in order: Domain, Application, Infrastructure, database, Api, local examples, tests.
5. Run the gates from `references/quality-gates.md`: build, AOT publish, all tests, coverage.
6. Report with the skill's report template: changes, behaviour, tests, real command results and follow-ups.

Do not skip the AOT publish. Do not report a command as passed unless it ran and exited 0.
