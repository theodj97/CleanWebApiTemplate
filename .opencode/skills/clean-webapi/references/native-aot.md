# Native AOT

The API publishes as Native AOT. A green build is not a green publish: only `dotnet publish` runs the AOT and
trimming analyzers against the real graph. This reference lists the rules, the banned APIs and the fixes for
the warnings you will meet.

## Project properties

`src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj`:

```xml
<PublishAot>true</PublishAot>
<IsAotCompatible>true</IsAotCompatible>
<EnableAotAnalyzer>true</EnableAotAnalyzer>
<EnableRequestDelegateGenerator>true</EnableRequestDelegateGenerator>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

`Application`, `Domain` and `Infrastructure` set `<IsAotCompatible>true</IsAotCompatible>` so the analyzer
checks the libraries too. `ServiceDefaults` does not publish; it is shared runtime code.

`Directory.Build.props` sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `EnableNETAnalyzers`,
`NuGetAudit` and `NuGetAuditMode=all`. A warning is a failure. Do not move the AOT properties to another
project to silence the analyzer.

`Microsoft.CodeAnalysis.BannedApiAnalyzers` is wired into every `src/` project with `BannedSymbols.txt` as an
`AdditionalFiles` entry. The rule is RS0030, promoted to an error by `TreatWarningsAsErrors`. Tests are not
analyzed by it.

## Banned APIs

`BannedSymbols.txt` blocks:

- `Activator.CreateInstance(Type)` and `(Type, object[])`;
- `Type.GetType(string)`, `GetMethod`, `GetProperty`, `GetMember`, `MakeGenericType`;
- `Assembly.Load`, `Assembly.GetType`, `Assembly.GetTypes`, `Assembly.GetExportedTypes`;
- `MethodInfo.Invoke`, `MakeGenericMethod`, `PropertyInfo.GetValue/SetValue`;
- `Enum.GetValues(Type)` / `Enum.GetNames(Type)` (the non-generic overloads);
- `JsonSerializer.Serialize/Deserialize` with a `Type` or with the reflection options.

Use the generic alternatives: `Enum.GetValues<TEnum>()`, `Enum.IsDefined<TEnum>()`,
`JsonSerializerContext`, static abstract interface members, `[LoggerMessage]`.

## Source-generated JSON

Two contexts, both required:

- `Api/{Feature}/{Feature}JsonSerializerContext` — requests, responses and collection types per feature.
- `Api/Configuration/AppJsonSerializerContext` — `ProblemDetails` (and anything shared).
- `Infrastructure/Secrets/OpenBaoJsonSerializerContext` — the Vault payloads.

The Api chains the feature contexts in front of the shared one:

```csharp
services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
```

Explicit type info at the call site:

```csharp
await response.Content.ReadFromJsonAsync(OpenBaoJsonSerializerContext.Default.OpenBaoReadResponse, ct);
request.Content = JsonContent.Create(body, OpenBaoJsonSerializerContext.Default.OpenBaoWriteRequest);
await context.Response.WriteAsJsonAsync(problem, AppJsonSerializerContext.Default.ProblemDetails, …);
```

`JsonContractTests` (unit) fails when a public `*Request`/`*Response` is missing from the feature context.
Component tests do **not** catch it: the test host's runtimeconfig keeps reflection-based JSON enabled. Only
the functional suite (separate process, `IsReflectionEnabledByDefault=false`) and the AOT binary do.

## Safe patterns already in the codebase

| Instead of | Use |
|---|---|
| `Enum.GetValues(Type)` | `Enum.GetValues<TEnum>()`, `Enum.IsDefined((TEnum)value)` |
| Reflection over handlers | `AddUseCase<TInput, TResult, THandler, TValidator>()` with `[DynamicallyAccessedMembers(PublicConstructors)]` on the implementation type parameters |
| Factories with `Activator` | static abstract members: `IResultFactory<T>.Failure`, `IBaseResponse<TDto, TResponse>.ToResponseModel` |
| String interpolation in logs | `[LoggerMessage]` partial methods |
| Reflection mapping | explicit `Map{Feature}(NpgsqlDataReader)` |
| Reflection JSON | `JsonSerializerContext` + explicit `JsonTypeInfo` |
| Typed lambda endpoints | method groups (`TodoEndpoints.GetByIdAsync`), request delegate generator enabled |

## Common warnings and fixes

| Warning | Cause | Fix |
|---|---|---|
| `IL2026` / `IL3050` | A reflection or dynamic-code API is reachable. | Replace it with a source-generated or static-abstract alternative; never suppress. |
| `IL2091` | A generic parameter passed to `AddTransient<TService, TImplementation>` lacks `PublicConstructors`. | Annotate the type parameter (`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]`). |
| `IL2026` in OpenAPI | A reflection-based schema generator. | Use `Microsoft.AspNetCore.OpenApi` with document/operation transformers and static schema helpers. |
| `IL2026` in JSON | `JsonSerializer.Serialize(value)` without a `JsonTypeInfo`. | Pass the context entry. |
| Build ok, publish fails | The AOT analyzer runs only on publish. | Run the publish command; do not claim the change is done without it. |

Do not add `RequiresDynamicCode`, `RequiresUnreferencedCode` or `UnconditionalSuppressMessage`. A suppression
hides the cause and the next publish fails elsewhere.

## Publish gate

```bash
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj \
  -c Release -r linux-x64 --self-contained
```

On Windows use `win-x64`. Expect zero warnings and a native binary under
`src/CleanWebApiTemplate.Api/bin/Release/net10.0/{rid}/publish/`. The publish takes minutes; run it after the
build and before reporting the change as done.

## Packages

The template chooses AOT-safe packages only. The OpenBao client is hand-written because the available Vault
clients depend on reflection or Newtonsoft.Json. Before adding a package, check that it is trimming/AOT
compatible; if it is not, write the small adapter instead of suppressing the warning.
