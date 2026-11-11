# Explicit registration and feature modules

There is no assembly scanning and no MediatR. Each use case is registered with
`AddUseCase<TInput, TResult, THandler, TValidator>()`, and each feature with `Add{Feature}Module()`, which
registers its repository, use cases, route group, JSON context and OpenAPI transformer. The reasons are
Native AOT (every type must be statically visible), discoverability (one file lists what a feature needs) and
startup validation (every use case is built once before the first request).

## Consequences

- Adding a feature is one module call in `ConfigureServices`; removing it is deleting one folder per layer and
  that call.
- A new use case that forgets its validator fails at startup, not on the first request.
- A contract missing from the feature's `JsonSerializerContext` fails `JsonContractTests` and the AOT binary.
- There is no convention to learn for "where is this registered": it is in the module.
