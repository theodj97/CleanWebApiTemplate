# ADO.NET over an ORM

Repositories use Npgsql's ADO.NET API directly — fixed SQL, bound parameters, an explicit row mapper — instead
of EF Core or Dapper. The reason is Native AOT (reflection-based mapping is unavailable or fragile) plus the
wish to keep the SQL visible and reviewable.

## Consequences

- Every query, parameter and column is written by hand; a new column touches the table constants, the column
  list, the INSERT/UPDATE parameters and the mapper.
- There is no change tracker and no lazy loading: each port method declares exactly what it reads.
- Transactions are explicit and confined to a repository method; no unit of work crosses layers.
- The `NpgsqlDataSource` is the single connection pool; repositories open a connection per operation.
