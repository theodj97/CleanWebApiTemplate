# Native AOT for the API

The API publishes with `PublishAot` so a deployment is a single native binary with predictable startup and no
runtime JIT. The cost is that reflection, `dynamic`, reflection-based JSON and runtime schema generation are
unavailable; the codebase uses source-generated JSON, static abstract interface members, explicit DI and
static OpenAPI transformers instead.

## Consequences

- A green build is not enough: `dotnet publish -c Release -r <rid> --self-contained` is the gate.
- A dependency that is not trim/AOT compatible is replaced by a small adapter (see the OpenBao client) instead
  of being suppressed with `RequiresDynamicCode`/`UnreferencedCode`.
- `BannedSymbols.txt` blocks the reflection APIs at compile time, and `IsAotCompatible` extends the analyzer to
  the libraries.
