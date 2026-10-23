# Observability and dashboard verification

The dashboard must answer "what happened to this request?", not merely open. This applies to Local mode; the test
modes are covered at the end ([Observability in test modes](#observability-in-test-modes)).

## Decide how services get telemetry

1. **Existing shared telemetry setup** (an extension method that registers OpenTelemetry, health checks, resilience):
   reuse it. Only add what is missing, typically the OTLP exporter honoring `OTEL_EXPORTER_OTLP_ENDPOINT`, which
   Aspire injects.
2. **Nothing comparable exists**: add a ServiceDefaults project.

   ```bash
   dotnet new aspire-servicedefaults -n {App}.ServiceDefaults -o src/{App}.ServiceDefaults -f <tfm>   # if templates installed
   dotnet sln <solution> add --solution-folder src src/{App}.ServiceDefaults/{App}.ServiceDefaults.csproj
   dotnet add src/{App}.Api reference src/{App}.ServiceDefaults
   ```

   Without templates, write the equivalent by hand. It is a class library with `<IsAspireSharedProject>true</…>`, a
   `FrameworkReference` to `Microsoft.AspNetCore.App`, and packages `Microsoft.Extensions.Http.Resilience`,
   `Microsoft.Extensions.ServiceDiscovery`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
   `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `.Http` and `.Runtime`. Versions
   come from the current template or NuGet, in `Directory.Packages.props` under CPM.

   Why `src/`: application projects call it in every environment, so it is shared runtime code. Keep it free of
   unrelated shared code and of any local orchestration.

Wire each service:

```csharp
builder.AddServiceDefaults();   // after CreateBuilder
…
app.MapDefaultEndpoints();      // after Build (web projects)
```

What the 13.6 template does:

- OTLP export only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so there is no effect outside Aspire unless configured.
- `/health` and `/alive` excluded from traces.
- Health checks with a `self` liveness check.
- Service discovery and the standard resilience handler on every `HttpClient`.
- `MapDefaultEndpoints` maps `/health` and `/alive` **only in Development**.

Each of these has consequences:

- A service started in another environment has no `/health`, so `WithHttpHealthCheck("/health")` reports Unhealthy.
  Check the environment Aspire actually gives it, which comes from the selected launch profile (`apphost.md` §
  Modelling resources).
- The global resilience handler changes `HttpClient` behaviour (retries, timeouts). If clients already have Polly
  policies, remove `AddStandardResilienceHandler` from the defaults or skip those clients, and report it, so policies
  are not stacked.
- Workers without ASP.NET Core can reference the same project (it carries the framework reference) or a
  hosting-only variant. Follow the repository's choice.

## Avoid duplicate pipelines

- Serilog: either keep Serilog and add its OpenTelemetry sink with the OTLP endpoint, or use `builder.Logging.AddOpenTelemetry`.
  Do not do both, because logs would appear twice.
- Application Insights or another exporter already configured for production: keep it, add OTLP for local only, and
  make sure the same instrumentation is not registered twice (`AddAspNetCoreInstrumentation` in two places).
- One `AddOpenTelemetry()` composition per service. Additional `WithTracing(t => t.AddSource(...))` calls for custom
  sources are fine.

## Dependency instrumentation

Without it, the trace for `POST /orders` is a single server span: the database work is invisible. In a verified run,
an API using `UseNpgsql` with ServiceDefaults produced only the HTTP span, while the SQL showed up only as log
lines. Options:

- **Aspire client integrations** (`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Aspire.Microsoft.EntityFrameworkCore.SqlServer`,
  `Aspire.StackExchange.Redis`, `Aspire.RabbitMQ.Client`, …). They register the client, health check, tracing and
  metrics in one call (`builder.AddNpgsqlDbContext<T>("orders")`). Adopt them only when they fit the existing
  registration: same connection key, and no lost custom options (`EnableRetryOnFailure`, interceptors, naming
  conventions). They read `ConnectionStrings:<name>`, so production configuration keeps working. They do add a
  dependency to an application project, so mention it.
- **Instrumentation libraries** next to the existing registration: `Npgsql.OpenTelemetry` (`.AddNpgsql()`),
  `OpenTelemetry.Instrumentation.SqlClient`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`,
  `OpenTelemetry.Instrumentation.StackExchangeRedis`, and the broker libraries' own `ActivitySource` names. This is
  the smallest change when the registration must stay as it is.
- **Custom work**: `ActivitySource` plus `AddSource(name)` for background jobs and migration runners.

Health checks for dependencies (`AddDbContextCheck`, `AddNpgSql`, …) make `/health` meaningful. Without them, a
healthy API only means the process is alive.

## Verification (record real output)

Run these against the running AppHost (always pass `--apphost <AppHost.csproj>` when several exist):

```bash
aspire describe --format Json            # redact "environment" values before showing anything
aspire wait <resource> --timeout 240     # exit code 0 = reached status (default: healthy)
curl -s -o /dev/null -w '%{http_code}\n' <endpoint from describe urls>/<real route>
aspire otel traces <resource> --format Json --limit 10
aspire otel logs <resource> --format Json --limit 20 [--severity Warning]
aspire logs <resource> --tail 50
```

| Claim | Evidence required |
|---|---|
| Resources are up | `describe`: expected `state` (`Running`, `Finished` with exitCode 0 for one-shot jobs) and `healthStatus` |
| Health is meaningful | The health endpoint exists in the environment, and dependency checks are registered (or report that only liveness is checked) |
| Logs available | `aspire otel logs <resource>` returns structured entries (console-only output in `aspire logs` means OTLP logging is not wired) |
| Traces produced | A trace whose `title`/span `http.route` matches the request you sent, with `http.response.status_code` |
| Dependency visibility | Child spans for DB/cache/HTTP calls in that trace, or an explicit "not instrumented" finding |
| Cross-service correlation | One `traceId` containing spans from both services for a call between them |
| Metrics | Visible in the dashboard Metrics page after exercising the path; the CLI has no metrics query in 13.6.x, so say how it was checked |

Notes:

- 4xx responses are not span errors: `hasError` is false for a 404. Look at `http.response.status_code`.
- Telemetry export is batched. If a trace is missing right after a request, retry after a few seconds before
  concluding that it is absent.
- "No errors in the dashboard" and "no instrumentation" look the same. Always confirm that the path produces spans
  before reporting it as healthy.

## Dashboard access and security

- `aspire start --format Json` and `aspire ps --format Json` return `dashboardUrl` with a `?t=<token>` login token.
  Never commit, log or paste it. In documentation write "use the dashboard link printed by `aspire run`/`aspire
  start`, or `aspire ps`".
- Keep authentication on. Do not set `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` or bind the dashboard to non-loopback
  addresses for local development.
- `aspire export --output <zip>` captures telemetry and resource data for sharing a diagnosis. It may contain
  sensitive data, so review it before sharing.

## Secrets in telemetry

Never record full JWTs, private keys, signing secrets, passwords or connection strings with credentials in logs,
traces, metrics or exception messages, in the applications, the AppHost, the tools or the tests. Log the token's
subject, issuer or `jti` when needed, never the token. `devtoken` writes the token only to stdout (its delivery
channel) and keeps diagnostics on stderr without secrets.

## Observability in test modes

The rule of the former testing skill is kept: tests need containers, not observability.

- The fixtures disable the dashboard (`DisableDashboard = true` in `DistributedApplicationTestingBuilder`).
- Every project the AppHost runs in a test mode (the applications in Functional mode and the DbInitializer in both
  test modes) gets `OTEL_EXPORTER_OTLP_ENDPOINT` set to an empty value, so the ServiceDefaults OTLP exporter stays off
  even if something injects an endpoint. In Component mode the application runs in-process without that variable and
  without a dashboard to send to.
- Test projects never reference ServiceDefaults or OpenTelemetry packages. The applications keep calling
  `AddServiceDefaults()` because it is their production code, and so does the DbInitializer, a local worker (not a test
  project) that the AppHost silences as above.
- Diagnosing a failing test, **before changing the test**: the fixture's error (resource name, state, exit code), the application's console logs
  (Aspire `ResourceLoggerService` from `App.Services`, or the test run output), and the test-mode safety nets
  (`BlockExternalHttpHandler`, `OutboundCallRecorder`). A failure that only reproduces in functional tests usually
  points to configuration (a key not injected, the trust mapping, a missing topic).
- When deeper diagnosis needs traces, reproduce the scenario in Local mode, where the dashboard and `aspire otel` are
  available, instead of enabling telemetry in the test modes.
