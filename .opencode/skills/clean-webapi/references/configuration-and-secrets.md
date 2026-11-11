# Configuration and secrets

Configuration is built deterministically, validated at startup, and holds no secrets. Every secret comes from
OpenBao in every mode.

## Sources and precedence

`AppConfigurationHelper.ConfigureSources` clears the sources the slim builder added and rebuilds them:

```text
appsettings.json
appsettings.{Environment}.json          (optional; Environment is Development, Production, Staging…)
user secrets                            (Development only)
OpenBao secret                          (skipped when OpenBao:Address is empty)
environment variables
command line
```

Later sources win. A separate **bootstrap** configuration (same files, user secrets, environment, command
line) exists only to locate OpenBao, because its address and token must be read before the OpenBao source is
added.

Environment files are `appsettings.Production.json`, `appsettings.Staging.json`,
`appsettings.Integration.json` — the name must match `ASPNETCORE_ENVIRONMENT` exactly. On Linux the file
system is case-sensitive: `appsettings.PRODUCTION.json` would never load.

## Binding and validation

`LoadAppSettings` binds manually — the reflection-based `ConfigurationBinder` is not AOT friendly:

```csharp
var appSettings = new AppSettings
{
    ConnectionStrings = new ConnectionStringsSection
    {
        Postgres = builder.Configuration.GetSection(nameof(AppSettings.ConnectionStrings))
                                      [nameof(ConnectionStringsSection.Postgres)] ?? string.Empty,
    },
    CorsAllowedOrigins = GetStringArray(builder.Configuration, Constants.CorsAllowedOrigins),
    // …
};
```

`AppSettingsValidator` (`IValidateOptions<AppSettings>`) rejects a missing connection string, an empty
`ValidIssuers`, a non-HTTPS/URN issuer, an empty `Audience`, and throws `OptionsValidationException` before
the host starts. `AuthenticationSettings.Load` validates the auth mode separately.

`SectionBase` derives a section name from its class name minus the `Section` suffix
(`ConnectionStringsSection` → `ConnectionStrings`). A new section class inherits it.

## Keys

| Key | Meaning |
|---|---|
| `ConnectionStrings__Postgres` | Value written by `openbao-init` as `ConnectionStrings:Postgres`. The API reads it from OpenBao. |
| `OpenBao__Address` | Base address; empty disables the OpenBao source. |
| `OpenBao__Token` | Token sent as `X-Vault-Token`. |
| `OpenBao__Mount` | KV v2 mount; default `secret`. |
| `OpenBao__Path` | Secret path, one per mode: `cleanwebapitemplate/local`, `…/component`, `…/functional`. |
| `OpenBao__TimeoutSeconds` | Per-request timeout; default 5. |
| `API_KEY` | HS256 signing key, **stored in OpenBao** as a secret entry. At least 32 UTF-8 bytes. |
| `Authentication__Mode` | `Symmetric` (alias `local`) or `Oidc`. Empty means inferred. |
| `Authentication__Authority` | OIDC authority (absolute HTTPS). Must be empty in Symmetric mode. |
| `Audience`, `ValidIssuers__N` | Token validation; supplied per run by the AppHost. |
| `Cors__AllowedOrigins__N`, `Cors__AllowCredentials` | Browser origins; empty disables CORS. |
| `OpenApi__Enabled` | Serves `/openapi/v1.json`; true in Development, false in Production. |

AppHost-only settings:

| Key | Meaning |
|---|---|
| `RunMode` | `Local` (default), `Component` or `Functional`. |
| `LocalAuth:Algorithm` | `HS256`; the AppHost rejects anything else. |
| `LocalAuth:Issuer`, `LocalAuth:Apis:{resource}:Audience`, `LocalAuth:Apis:{resource}:RoleClaim` | Token trust values from `local/tools/devtoken.json`. |
| `Parameters:local-jwt-signing-key` | HS256 key, written by `devtoken init` into AppHost user secrets. |
| `Parameters:openbao-root-token` | Optional OpenBao root token for a test run. |
| `Testing:OutboundProxy` | Proxy URL the functional fixture records. |

db-init reads `ConnectionStrings__Postgres`, `Schema__Path` and, in Local mode, `Seed__Path`. The Swagger
sidecar reads `API_BASE_URL`, `EXAMPLES_PATH` and the `LocalAuth__*` values.

## OpenBao

`Infrastructure/Secrets` is a hand-written KV v2 client, because the available .NET Vault clients are not AOT
safe:

- `OpenBaoClient` — `ReadAsync(path)` returns the string entries of `data.data`; `WriteAsync(path, values)`
  replaces the secret. Errors become `OpenBaoException` with the server's `errors` list, never the token.
- `OpenBaoOptions` — `Address`, `Token`, `MountPath`.
- `OpenBaoConfigurationProvider` — reads the secret **once**, while the host configuration is built. There is
  no watcher and no reload: restart to pick up a rotated secret.
- `OpenBaoConfigurationSource` — builds one `HttpClient` and one client per process.
- `OpenBaoConfigurationKeys` — the key names and defaults (`secret`, 5 s).
- `AddOpenBaoSecrets(bootstrap)` — no-op when the address is empty; throws with a clear message when the
  address is set but the token, path or timeout is missing or invalid.
- `OpenBaoJsonSerializerContext` — the source-generated metadata for the Vault payloads.

### The local flow

1. `AppHost` starts `openbao/openbao:2.7.2` unsealed in dev mode, in memory, no volume.
2. `openbao-init` (`local/aspire/…OpenBaoInitializer`) receives `OpenBao__*`, `API_KEY` and
   `ConnectionStrings__Postgres`, and writes both values as one secret at `OpenBao:Path`.
3. The application receives only `OpenBao__Address`, `OpenBao__Token`, `OpenBao__Mount` and `OpenBao__Path`.
4. At startup `AddOpenBaoSecrets` reads the secret and exposes each entry as a configuration key
   (`API_KEY`, `ConnectionStrings:Postgres`, …).
5. The connection string reaches `AppSettings.ConnectionStrings.Postgres`; `API_KEY` reaches
   `AuthenticationSettings`.

The component fixture points the in-process application at the same server and asserts that both values came
from OpenBao. The functional API process does the same through the environment.

### Adding a secret

1. Decide the configuration key and how it is validated at startup (`AppSettingsValidator` or a dedicated
   settings type).
2. Write it in `local/aspire/…OpenBaoInitializer/Program.cs` (add the constant, the environment variable and
   the dictionary entry) and in the AppHost resource (`OpenBao.cs`): pass the value as
   `WithEnvironment(key, …)` from a parameter or user secret.
3. Do **not** pass the value to the application as an environment variable; pass only `OpenBao:*`.
4. Read it from configuration in the composition root and validate it before `Build`.
5. Update the component fixture only if the value is not already written by `openbao-init`; extend
   `EnsureTestConfigurationIsEffective` to prove it came from OpenBao.
6. Document the key by name in the README; never the value.

## Environment names in tests

There is no `appsettings.Testing.json`. The component fixture sets `ASPNETCORE_ENVIRONMENT=Testing` and passes
the JWT trust, OpenBao and log levels as in-memory settings and process environment variables; the functional
AppHost sets them on the application resource. A missing key fails
`EnsureTestConfigurationIsEffective` with a message naming it.
