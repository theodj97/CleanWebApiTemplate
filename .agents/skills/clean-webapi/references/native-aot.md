# Native AOT

The API project is published with Native AOT. `PublishAot`, `IsAotCompatible`, `EnableAotAnalyzer`,
`EnableRequestDelegateGenerator` and `TreatWarningsAsErrors` are not negotiable: do not relax them, move them
to another project or suppress the analyzer to make a change compile.

The real gate is:

```bash
dotnet publish src/{App}.Api/{App}.Api.csproj -c Release -r linux-x64 --self-contained
```

Use `win-x64` (or the target RID) on Windows. A successful `dotnet build` is **not** a successful AOT publish;
the analyzer and the publish use different roots and the publish catches things the build does not.

## What breaks AOT

| Pattern | Why | Fix |
|---|---|---|
| `System.Reflection` (`Assembly.Load`, `Type.GetType`, `GetMethod`, `MakeGenericType`, `Activator.CreateInstance`) | Types are trimmed or unavailable | Static code, explicit registrations, source generators |
| `dynamic` | Requires the runtime binder | Typed models, `JsonDocument`/`JsonNode` with a source-generated context |
| `JsonSerializer.Serialize(object)` / reflection options | Reflection-based serializer is disabled | `AppJsonSerializerContext` + `JsonSerializer.Serialize(value, AppJsonSerializerContext.Default.Type)` |
| Swashbuckle `SwaggerGen` or runtime schema generation | Reflection-based | `Microsoft.AspNetCore.OpenApi` + static descriptors |
| Assembly scanning (`AddValidatorsFromAssembly`, `Assembly.GetTypes`) | Trimming cannot see the types | Explicit registration in `ConfigureServices` |
| `ConfigurationBinder` over arbitrary types | Reflection | Manual binding in `LoadAppSettings` (or a source-generated binder) |
| `Enum.GetValues`/`Enum.GetNames` in generic code | Generic reflection can be trimmed | A static array of values, or `Enum.IsDefined(typeof(T), value)` where the analyzer accepts it |
| `MakeGenericType`/generic math over runtime types | Runtime code generation | Concrete generic instantiations |
| A package without AOT annotations | Its reflection paths are trimmed | Replace it, use a source-generated alternative, or isolate it behind a compiled path |
| `RequiresDynamicCode`/`RequiresUnreferencedCode`/`UnconditionalSuppressMessage` | Hides the problem | Fix the cause; do not suppress |

`BannedSymbols.txt` already blocks the reflection APIs and the reflection-based JSON overloads. The analyzer
flags them as errors.

## JSON

`Api/Configuration/AppJsonSerializerContext.cs`:

```csharp
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(IEnumerable<ProjectResponse>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
```

- Add every request, response, collection, scalar and error shape the API serialises/deserialises.
- The context is inserted at the front of the resolver chain in `ConfigureServices`.
- The source generator produces metadata at compile time; the type must be reachable statically.
- For a type from a library, add `[JsonSerializable(typeof(LibraryType))]`; do not fall back to reflection.
- `JsonSerializerOptions.Web` (camelCase) is set by `JsonSourceGenerationOptions`.

A missing entry usually shows up at runtime as a `NotSupportedException`/`InvalidOperationException` about
metadata, not at build time. Add a component test that calls the endpoint; the test host uses the same context.

## Minimal APIs

- `EnableRequestDelegateGenerator` is on. Use typed lambda parameters; the generator creates the request
  delegate at compile time.
- Avoid `Delegate`/`MethodInfo` binding, `MapMethods` with a runtime-built delegate, or a custom
  `RequestDelegate` built by reflection.
- `MapGroup` + `IGroupMap` is the template's pattern; keep the endpoint delegate statically visible.
- `Results.Ok`/`Results.Created`/`Results.Problem` are AOT-safe; custom `IResult` implementations must write
  JSON through the context.

## OpenAPI

- `AddApiOpenApi` + `MapOpenApi`.
- `OpenApiConfiguration` sets the document metadata and security.
- `{Feature}OpenApiSchemas` patches the generated schemas with static descriptors.
- `AddOpenApiOperationTransformer` is used for small metadata fixes; it must not download schemas or use
  reflection over arbitrary types.
- No Swashbuckle `SwaggerGen`, no `NSwag` runtime generation.

XML documentation is generated for the API project (`GenerateDocumentationFile`) and feeds the OpenAPI
document. Add XML comments to public request/response members.

## Common warnings

| Warning | Typical cause | Fix |
|---|---|---|
| `IL2026` | A member marked `RequiresUnreferencedCode` is called | Replace it with an AOT-safe API; if it is a false positive in `dotnet format analyzers` for minimal APIs, rely on the publish gate (see below) |
| `IL3050` | A member marked `RequiresDynamicCode` is called | Replace it; do not suppress |
| `IL2091` | A generic parameter is not preserved | Use a concrete type in the JSON context/registration |
| `IL2075` | A member is accessed through reflection | Static access or explicit mapping |
| `IL2026` on `JsonSerializer` | Reflection-based serialization | Use the source-generated context |
| `IL2026` on `ConfigurationBinder` | Reflection binding | Manual binding |
| `IL3050` on `MakeGenericType` | Runtime generic construction | Concrete generic types |
| `IL2026` on a package | The package is not AOT-compatible | Find an alternative or isolate it behind a source-generated path |

`dotnet format analyzers` excludes `IL2026` and `IL3050` in `scripts/pre-push.sh` because the request-delegate
source generator does not run in that mode and reports false positives on minimal APIs. That exclusion does
**not** make the warnings acceptable: the `dotnet publish` gate is the authority. Never add a suppression to
make `dotnet format analyzers` pass; run the publish.

## Packages

Before adding a package to `Directory.Packages.props`:

1. Check its AOT/trimming annotations and documentation.
2. Prefer a package with a source generator or a documented AOT path (Npgsql, `Microsoft.AspNetCore.OpenApi`,
   FluentValidation without the assembly scanner, `Microsoft.IdentityModel.JsonWebTokens`).
3. Avoid a package whose convenience API is reflection-based; use its lower-level API if it is annotated.
4. Build **and** publish with the new package before considering the change done.
5. If the package cannot be AOT-safe, report it as a blocker and propose an alternative; do not add
   `RequiresDynamicCode` or suppress the warning.

## Trimming

`PublishAot` implies trimming. `TrimmerRootAssembly`/`TrimmerRootDescriptor` are not a substitute for explicit
code; avoid them unless a package documents a root descriptor and the user approves it. Adding a root
descriptor to keep reflection working is a red flag: replace the reflection instead.

## Checklist

- [ ] No new `System.Reflection`, `dynamic`, reflection JSON or reflection OpenAPI.
- [ ] Every new contract type is in `AppJsonSerializerContext`.
- [ ] Every new handler, validator, `ValidatorSet<T>` and repository is registered explicitly; `ValidateOnBuild`
      catches an empty set at startup.
- [ ] No `RequiresDynamicCode`, `RequiresUnreferencedCode` or `UnconditionalSuppressMessage` added.
- [ ] New packages checked for AOT compatibility.
- [ ] `dotnet build` passes with zero warnings.
- [ ] `dotnet publish ... -c Release -r <rid> --self-contained` passes with zero warnings.
- [ ] A component test exercises the new endpoint (catches missing JSON metadata at runtime).
- [ ] `BannedSymbols.txt` unchanged unless the user explicitly approves a new banned API.
