# ULID stored as `bytea`

Identifiers are ULIDs (26-character Crockford Base32 strings, sortable by creation time) and PostgreSQL stores
them as `bytea` (16 bytes). The API serializes them as strings with an OpenAPI `format: ulid`, the repository
maps `Ulid.ToByteArray()` / `new Ulid(byte[])`.

## Consequences

- Ids sort by creation time and are safe to expose.
- The database uses 16 bytes per key instead of 26+ and an index on `bytea`.
- A query parameter binds `NpgsqlDbType.Bytea`, never text; a filter that accepts ids parses them to `Ulid`
  before reaching the repository.
- The seed writes fixed ids with `decode('<hex>', 'hex')`.
