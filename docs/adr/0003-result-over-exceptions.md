# Result/Error instead of exceptions for business outcomes

Handlers and repositories return `Result<T>` with a typed `Error` (`BadRequestError`, `NotFoundError`,
`ConflictError`, …). Only unexpected failures throw; the exception handler turns them into a 500
ProblemDetails. This keeps "the request was understood and refused" out of the exception path and makes the
possible outcomes part of the signature.

## Consequences

- `ApiResultExtensions` is the only place that maps an error to a status; a new error subtype needs a mapping
  and a test.
- Validation returns `BadRequestError` from `ValidatorSet`; a handler never throws for an invalid input.
- A repository maps the expected unique-index violation to `ConflictError` (checking the constraint name) and
  lets every other `PostgresException` propagate.
