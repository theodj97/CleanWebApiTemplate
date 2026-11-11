# Forward-only SQL scripts

Schema and seed live in numbered SQL files under `local/database` and are applied in ordinal order. A script
that has been deployed is never edited: a change to an existing table is a new, higher-numbered script with
`ALTER TABLE … ADD COLUMN IF NOT EXISTS` (or the equivalent). The rule applies from the first deployment; the
template's own `0001` may still change before that point.

## Consequences

- Any runner can apply the scripts (the template's `db-init`, Flyway, DbUp, a DBA); the repository does not
  depend on a migration tool.
- The local database is recreated on every start, so the scripts must also be re-runnable (`IF NOT EXISTS`,
  `ON CONFLICT DO NOTHING`).
- A column change touches the entity, DTO, request/response, validator, table constants, mapper, a new SQL
  script, the seed, the OpenAPI descriptor and the tests, in that order.
