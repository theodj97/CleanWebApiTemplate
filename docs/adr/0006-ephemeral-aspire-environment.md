# Ephemeral Aspire environment

> Superseded in part by [0010](0010-reused-test-postgres.md): test modes reuse a PostgreSQL container. Local mode
> and OpenBao remain ephemeral.

PostgreSQL and OpenBao run as containers without volumes. Every AppHost start (Local or test mode) begins with
empty containers, `db-init` reapplies the schema and, in Local mode, the seed; `openbao-init` rewrites the
secrets. Tests therefore never depend on state left by a previous run, and a SQL script can be validated by
restarting.

## Consequences

- Data does not survive `aspire stop`; that is intentional for local development and tests.
- The seed runs only in Local mode (`Seed__Path` is passed only there); tests start from the schema.
- Tests reset rows with Respawn between tests, not by recreating containers.
- A deployed database is outside this decision: it uses the same forward-only scripts with an external runner
  or a DBA.
