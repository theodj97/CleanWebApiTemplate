# Configuration and secrets

## Rules

- `appsettings*.json` contain no secrets and no connection strings. They may contain non-sensitive defaults
  (CORS origins, OpenAPI flag, issuer/audience, logging).
- Secrets come from OpenBao at startup, or from the platform in a deployed environment. The AppHost passes only
  `OpenBao:*` (address, token, mount, path) to the application, never the secret values.
- Configuration is bound manually, not with the reflection-based `ConfigurationBinder`. The AOT analyzer must
  see every property.
- Invalid settings fail at startup with a clear message, not on the first request with a `500`.

## Precedence

`AppConfigurationHelper.ConfigureSources` rebuilds the sources deterministically. Later sources win:

```text
appsettings.json
→ appsettings.{Environment}.json
→ user secrets (Development only)
→ OpenBao (when OpenBao:Address is set)
→ environment variables
→ command line
```

`OpenBao` is located through a bootstrap configuration (`bootstrapBuilder`) that reads the JSON files, user
secrets (Development), environment and command line. That is how `OpenBao:Address`/`OpenBao:Token` can arrive
from the environment without being known before the source is added.

When `OpenBao:Address` is empty, `AddOpenBaoSecrets` is a no-op. This is why unit tests and the component test
mode can start without OpenBao; the component fixture still points at the real container and asserts the
secret came from it.

## AppSettings

`Api/Configuration/AppSettings.cs` is a plain class with `required` properties:

```csharp
public sealed class AppSettings
{
    public required ConnectionStringsSection ConnectionStrings { get; init; }
    public required string[] CorsAllowedOrigins { get; init; }
    public required bool CorsAllowCredentials { get; init; }
    public required bool OpenApiEnabled { get; init; }
    public required string[] ValidIssuers { get; init; }
    public required string Audience { get; init; }
}
```

Sections derive from `SectionBase`. The class name must end in `Section`; `SectionBase` removes the suffix to
form the configuration section name (`ConnectionStringsSection` → `ConnectionStrings`).

`AppConfigurationHelper.LoadAppSettings` binds each value manually, then runs `AppSettingsValidator` (an
`IValidateOptions<AppSettings>`). Add a new property in four places:

1. `Constants` (the key, if it is a cross-cutting key).
2. `AppSettings` (the property) and `LoadAppSettings` (the manual binding, including arrays/booleans).
3. `AppSettingsValidator` (required, format, range, combinations).
4. `appsettings.json` / `appsettings.{Environment}.json` (non-secret defaults) and the AppHost/environment
   (the value that changes per environment).

`AuthenticationSettings` is separate on purpose: it resolves the two JWT modes and their valid combinations
before the auth handler is registered. `AppSettings` holds the common issuer/audience; the mode-specific
authority and key live in `AuthenticationSettings`.

## Keys the template reads

| Key | Meaning | Source |
|---|---|---|
| `ConnectionStrings:Postgres` | PostgreSQL connection string | AppHost `WithReference(todoDb, "Postgres")`; environment in deployment |
| `OpenBao:Address` | OpenBao base address; empty disables the source | AppHost `WithOpenBaoSecrets` |
| `OpenBao:Token` | OpenBao token | AppHost per-run root token; platform in deployment |
| `OpenBao:Mount` | KV v2 mount, default `secret` | AppHost |
| `OpenBao:Path` | Secret path, per mode | AppHost |
| `OpenBao:TimeoutSeconds` | Single request timeout, default 5 | Optional |
| `API_KEY` | HS256 key, read from OpenBao in Symmetric mode | `openbao-init` |
| `Authentication:Mode` | `Symmetric` or `Oidc`; inferred from `Authentication:Authority` when empty | appsettings/environment |
| `Authentication:Authority` | OIDC authority; must be empty in Symmetric mode | appsettings/environment |
| `ValidIssuers` | Accepted JWT issuers (array) | appsettings/AppHost |
| `Audience` | Accepted JWT audience | appsettings/AppHost |
| `Cors:AllowedOrigins:N` | Browser origins; empty disables CORS | appsettings |
| `Cors:AllowCredentials` | Allow credentials | appsettings |
| `OpenApi:Enabled` | Serve `/openapi/v1.json` | appsettings (false in PRODUCTION) |
| `Logging:LogLevel:*` | Log levels | appsettings/environment |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint; empty disables export | AppHost |

## Adding a setting

1. Add the key to `Constants` if it is shared; add the property to `AppSettings` and bind it manually.
2. Add validation to `AppSettingsValidator` (required, URI, range, mutual exclusion).
3. Add the non-secret default to `appsettings.json`; add environment-specific values to
   `appsettings.{Environment}.json` only when they are not secrets.
4. Add the value to the AppHost (`WithEnvironment` or `WithReference`) or to the deployment configuration.
5. If the setting must reach the tests, add it to `AppHostContract.AppSettings` and to `TestAuthSettings` or the
   fixture; the component fixture asserts that the application reads the test value.
6. Update `README.template.md` / `README.md` configuration table and `local/local-development.md` when the
   value changes local behaviour.

**Done when:** an invalid value fails at startup with a message naming the key; the value is in no secret
store unless it is a secret; tests and local development read the same key the application reads.

## Adding a secret

A secret is a value that must not appear in Git, logs, test output or `appsettings*.json`.

1. Add the secret name to `local/aspire/{App}.OpenBaoInitializer/Program.cs` (or the OpenBao seeding code) and
   write it into the secret path. The initializer is idempotent: it runs on every start.
2. Add the value as an AppHost parameter (user secrets for local), for example
   `builder.AddParameter("my-secret", secret: true)`, or read it from the AppHost user secrets.
3. Pass the parameter to the initializer only; pass `OpenBao:*` to the application through
   `WithOpenBaoSecrets`.
4. Read it with `configuration["MY_SECRET"]` where it is needed; validate it at startup.
5. Add it to the test trust/settings only if the test must provide it; otherwise the test reads it from OpenBao
   like the application.
6. Document the name and purpose (never the value) in `README.template.md` and `local/local-development.md`.
7. Rotate with `dotnet local/tools/devtoken.cs init --rotate` for the local JWT key; for other secrets, change
   the AppHost user secret and restart.

**Done when:** `git grep` and the test output contain no secret value; the initializer exits 0; the
application reads the secret from OpenBao; no `API_KEY`/secret value is present in the test process
environment (the component fixture fails if it is).

## OpenBao adapter

`Infrastructure/Secrets/`:

- `OpenBaoConfigurationExtensions.AddOpenBaoSecrets` adds the source when `OpenBao:Address` is set.
- `OpenBaoConfigurationProvider` reads one KV v2 secret at startup and exposes its entries as configuration
  keys. There is no watcher, no lease and no reload: restart to pick up a rotated secret.
- `OpenBaoClient` is a small HTTP client with a source-generated JSON context; it never logs the token or the
  secret values.
- `OpenBaoOptions` holds address, token, mount and timeout.

Do not put secret values in `OpenBao:Path` (the path is not secret, the values are). Do not log
`OpenBao:Token`. Redact `aspire describe` output.

## Test contract

The AppHost and the tests share `AppHostContract.cs` (the file is compiled into `Tests.Shared` through a
linked `Compile` item):

- `RunMode` selects `Local`, `Component` or `Functional`.
- `Resources` names must stay stable; tests and docs use them.
- `AppSettings` holds the exact keys the API reads.
- `OpenBao` holds the address/token/mount/path keys and the per-mode path.
- `LocalAuth` holds the local JWT algorithm, issuer, signing-key parameter, audience and role-claim keys.
- `JwtTrust(resourceName)` is the single mapping from a resource to the configuration keys that make it trust
  the local issuer. Add an entry for every new application.

The component fixture fails if:

- a setting it passed is not read by the application (a misspelled key);
- `API_KEY` is present in the test process (it must come from OpenBao);
- a `JwtBearer` scheme would download metadata (an `Authority`/`MetadataAddress` was not cleared).

The functional fixture records every outbound HTTP call through `OutboundCallRecorder`; tests assert it stays
empty. A new external dependency must have a local substitute; do not relax the recorder.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `'API_KEY' is required in Symmetric mode` | OpenBao did not seed it, or `OpenBao:Path` is wrong. Check `openbao-init` exit code. |
| `'API_KEY' must be at least 32 UTF-8 bytes` | The key is too short; rotate with `devtoken init --rotate`. |
| `The application did not read 'API_KEY' from OpenBao` | A stray `API_KEY` in the test process, or the fixture passed it directly. |
| `The application does not read the test value for '<key>'` | The key name differs from what the app reads, or a later configuration source overrides it. |
| `JwtBearer scheme '…' would download metadata` | The trust mapping did not clear `Authority`/`MetadataAddress` (`TrustValue.Empty`). |
| Invalid settings only fail on the first request | The validator is missing a rule or the value is read after the host is built. |
