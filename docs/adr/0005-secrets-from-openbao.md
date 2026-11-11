# Every secret comes from OpenBao

The application receives only `OpenBao:*` as environment variables. The JWT signing key **and** the PostgreSQL
connection string are written into OpenBao by `openbao-init` in every mode (Local, Component, Functional) and
read at startup. `appsettings*.json` contains no secret and no connection string.

## Consequences

- A new secret is added to `openbao-init` and the AppHost, never to the application environment; tests assert
  that the value came from the store.
- The OpenBao client is hand-written because the available .NET clients are not AOT compatible.
- The secret is read once at startup: rotating it requires a restart. There is no lease renewal.
- In a deployment the server is external; only the `OpenBao:*` values change, not the code.
