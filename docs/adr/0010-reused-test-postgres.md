# Reused PostgreSQL in test modes

## Context

Component and functional tests each start the shared AppHost with a fresh PostgreSQL container. The container
startup and the initializers take about 13 s per suite and do not depend on the number of tests; the tests
themselves take seconds. Pulling the image, creating the database and applying the schema on every run is the
dominant cost of the integration suites.

The test suites must keep using real PostgreSQL and OpenBao; replacing them with fakes would lose the contract the
template exists to prove.

## Decision

In test modes (`Component` and `Functional`) the AppHost uses one persistent PostgreSQL container per mode and
checkout:

- The container name and data volume are derived from the checkout path and the machine name, so two clones do not
  share one and a checkout keeps reusing the same one.
- The container password is derived from the same values; it is a local test credential, is never committed and
  never reaches the application, which still reads the connection string from OpenBao.
- The schema scripts stay idempotent and `db-init` runs on every start; the fixture resets all tables once at
  startup to clear rows a previous run could have left.
- OpenBao stays ephemeral and per-run. Local mode stays fully ephemeral.
- `TESTCONTAINERS_REUSE=0` restores a fresh PostgreSQL container for tests.

## Consequences

- Fixture startup drops from ~13 s to ~10 s per integration suite; the full test suite drops from ~18 s to ~12 s
  when the containers are warm.
- Two runs of the same mode and checkout on one machine share the container and its database; use
  `TESTCONTAINERS_REUSE=0` in one of them or use a separate checkout.
- A PostgreSQL image upgrade or a corrupt volume can require removing the `clean-webapi-postgres-*` container and
  volume, or running with `TESTCONTAINERS_REUSE=0`.
- The local test credential is deterministic for a given checkout and machine. It is a disposable local container
  credential, not an application secret; production and Local mode keep their existing behavior.
