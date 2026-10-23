# Local development: running, configuring, AI agents and debugging

How the Local mode of the AppHost is operated: configuration and external services, the operational document, the
official Aspire agent tooling and the evidence-first debugging loop. Topology and modes are in `apphost.md`, local JWT
in `local-jwt-authentication.md`, telemetry in `observability.md`.

## Contents
- [Starting, stopping, inspecting](#starting-stopping-inspecting)
- [Configuration and external services](#configuration-and-external-services)
- [Getting tokens](#getting-tokens)
- [The operational document](#the-operational-document)
- [Official Aspire AI agent integration](#official-aspire-ai-agent-integration)
- [MCP (optional)](#mcp-optional)
- [Debugging loop](#debugging-loop)

## Starting, stopping, inspecting

```bash
aspire run   --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj     # foreground, Ctrl+C to stop
aspire start --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj     # background (add --isolated for a parallel copy)
aspire stop  --apphost local/aspire/{App}.AppHost/{App}.AppHost.csproj     # stops it; every database starts empty next time
aspire ps --format Json                                                    # AppHosts running on the machine
aspire describe --apphost <csproj> --format Json                           # states, health, exit codes, urls, env (redact values)
aspire wait <resource> --status healthy|up --apphost <csproj> --timeout 300
```

- Before starting anything, `aspire ps`: never stop or duplicate AppHosts you did not start (another repository's
  AppHost may be running).
- Running the AppHost project from Visual Studio, Rider or `dotnet run --project local/aspire/{App}.AppHost` also
  works.
- First start without the local JWT key fails fast with the command to run (`dotnet local/tools/devtoken.cs init`).
- The dashboard login link comes from `aspire run`/`aspire start` or `aspire ps --format Json` (`dashboardUrl`). It
  contains a personal login token: never commit, log or paste it.
- On Podman, Aspire needs `ASPIRE_CONTAINER_RUNTIME=podman`. The HTTPS development certificate may be untrusted
  (`dotnet dev-certs https --check --trust`); trusting it is a machine-wide change that needs consent
  (`aspire certs trust` or `dotnet dev-certs https --trust`). The applications' HTTP endpoints keep working without it.

## Configuration and external services

- Aspire injects connection information through `WithReference`; resource names match the keys the code already reads
  (`apphost.md` § Modelling resources). Do not duplicate generated values in `appsettings*.json`.
- External credentials (third-party APIs, corporate services) are secret parameters in the AppHost user secrets
  (`aspire secret set Parameters:<name> <value> --apphost <csproj>` or env `Parameters__<name>`), documented with how to
  obtain them. External services stay external in Local mode unless an official emulator exists and the code supports
  pointing at it; never fake a service that cannot reasonably be emulated, and list what does not work without it.
- **Exception: identity providers.** Authentication never depends on one, in Local mode either: protected services
  trust the local issuer (`local-jwt-authentication.md`).
- Sanitized examples only, under `local/`; populated local files in the existing `.gitignore`.
- Development-only code paths that the application already has (`IsDevelopment()` seeding, Swagger, exception pages)
  are normal in Local mode; one that bypasses authentication is a finding (`local-jwt-authentication.md` § Isolation).

## Getting tokens

Any HTTP client (Swagger/Scalar, Bruno, `.http` files, `curl`, PowerShell) gets a locally signed token from
`dotnet local/tools/devtoken.cs get --profile <name>`. Profiles, usage examples and key storage:
`local-jwt-authentication.md` § Local development: devtoken. Insufficient-permission profiles exist on purpose, to try
403 responses by hand.

## The operational document

Create or update `local/local-development.md` from `assets/local/local-development.md`, filling every placeholder with
verified values: commands, resource names and topology, database strategy, schema + seed process and how to add seed
scripts, token profiles and how to use them, external services and parameters, how to run the tests, agent workflow,
troubleshooting. Do not write dashboard URLs with tokens, guessed ports or results you did not observe. Respect an
existing equivalent document under `local/` (update it instead of creating a second one). Existing README/docs/scripts
that describe local setup must stay consistent: update them in the same change.

## Official Aspire AI agent integration

Official guidance: <https://aspire.dev/get-started/ai-coding-agents/>, <https://aspire.dev/get-started/aspire-skills/>,
<https://aspire.dev/dashboard/ai-coding-agents/>. Do not rewrite or duplicate the official Aspire skills: install them
and use the CLI evidence they describe.

1. **CLI and options.** `aspire --version`, then `aspire agent init --help`. Proceed only if the help lists `--skills`,
   `--skill-locations` and `--workspace-root`. If the CLI is missing or too old, report it with the official
   install/update instructions (`sources.md`) and do not claim the agent setup was done.
2. **Existing installations.** Look for Aspire skills (`aspire`, `aspire-init`, `aspire-orchestration`,
   `aspire-monitoring`, …) in `.claude/skills/`, `.agents/skills/`, `.github/skills/`, `.opencode/skill/`, nested
   folders such as `local/aspire/*/.claude/skills/`, and `.mcp.json`.
   - `aspire agent init` removes its skills from locations you do not select: include every location the repository
     already uses (`claudecode` → `.claude/skills/`, `standard` → `.agents/skills/`, `github` → `.github/skills/`,
     `opencode` → `.opencode/skill/`), unless the user wants to drop one.
   - A nested installation under the AppHost folder is only discovered when the agent touches that folder: replace it
     with a root installation, then remove the nested copy only after the root one is verified.
3. **Run from the repository root**, so the skills land next to the repository's other skills:
   ```bash
   aspire agent init --non-interactive --workspace-root . \
     --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring \
     --skill-locations claudecode            # add ,standard when the repo is shared with .agents/skills agents
   ```
   - Use an **explicit skill list**. In 13.6.1 `--skills all` also installs the `dotnet-inspect` and `playwright-cli`
     companions, and the latter runs a **global npm install of `@playwright/cli`**: add companions only when the user
     wants them and accepts that global install.
   - Add `aspire-deployment`, `aspireify` or `aspire-project-v2-migration` when the workflow needs them. The official
     docs recommend installing the workflow bundle together; local development needs at least the four above.
   - Skill names come from the bundle and may change: if the CLI rejects a name, list what it offers and adjust.
4. **Verify.** `ls .claude/skills/*/SKILL.md` shows the installed skills next to the custom ones, each with
   `name`/`description` frontmatter; custom skills (this one included) are untouched (`git status`); report the exact
   command, skills, locations and whether `.mcp.json` was created. Claude Code picks up skills in an existing
   `.claude/skills/` live; a directory new in this session needs `/reload-skills` or a new session: say which.
5. **Commit guidance.** Installed skill files are repository content: they can be committed, or ignored if each
   developer runs `aspire agent init`. Follow existing practice.

## MCP (optional)

The Aspire CLI already exposes resources, logs, traces and restarts, so MCP is **not** required. Configure it only
when the user wants agents to use MCP tools (for example an agent host without shell access) and opts in:

```bash
aspire agent init --non-interactive --workspace-root . --skills none --skill-locations claudecode --mcp
```

For Claude Code this writes `.mcp.json` with `{"mcpServers":{"aspire":{"command":"aspire","args":["agent","mcp"]}}}`.
STDIO only, no network port; it does not expose source code, environment variable values or secrets, but resource
logs can contain sensitive data; hide resources with `.ExcludeFromMcp()` when needed. Never pass `--mcp` silently;
pass `--mcp=false` to state the opt-out when re-running init.

## Debugging loop

Use it for `debug <symptom>` and whenever validation fails. Evidence first, then the smallest fix, then the same
scenario again.

1. **Inspect, don't duplicate.**
   ```bash
   aspire ps --format Json
   aspire describe --apphost <csproj> --format Json          # states, health, exit codes, urls, relationships
   aspire logs <resource> --apphost <csproj> --tail 100      # console output incl. "Waiting for resource …"
   aspire otel logs <resource> --apphost <csproj> --format Json --severity Warning
   aspire otel traces <resource> --apphost <csproj> --format Json --limit 20 [--has-error true]
   ```
   Redact `environment` values and any `dashboardUrl` token before quoting output.
2. **Start or wait only when needed.** `aspire start --apphost <csproj>` (background) only if not running; `--isolated`
   for a second worktree or parallel run. Then `aspire wait <resource> --status healthy|up --timeout <s>`: exit code 0
   means the status was reached; otherwise read `describe`/`logs` before retrying.
3. **Locate the failure.** Which resource is `FailedToStart`, `Exited` (exit code), `Unhealthy`, or stuck waiting (its
   log shows `Waiting for resource 'x'`)? For an HTTP failure, reproduce the request (with a devtoken token when the
   endpoint is protected), find its trace (`aspire otel traces <res> --search "<route>"` or `--trace-id <id>`), and
   correlate logs (`aspire otel logs <res> --trace-id <id>`). Passing health checks do not prove requests work.
4. **Hypothesis from evidence.** Tie logs and traces to code and configuration. Classify: application code,
   configuration or keys, dependency readiness (`WaitFor` missing), schema initialization, networking/endpoints,
   container runtime, certificates, token trust. Check the environment variable **names** a process received before
   blaming the code (missing `ConnectionStrings__x`, `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`,
   `OTEL_EXPORTER_OTLP_ENDPOINT`, the trust mapping keys).
5. **Fix and verify.** The smallest change. Restart only the affected resource (`aspire resource <res> restart`) when
   the AppHost model does not change (it keeps the database); changes to `AppHost.cs` need `aspire stop` + `aspire
   start` of the AppHost you started, which rebuilds every database from schema + seed, as designed (plain stop, never
   `--force --volumes`). Repeat the **original** failing scenario, check the new response, then the new trace/logs.
   "Healthy" alone is not verification.
6. **Report honestly.** Cause (confirmed or hypothesis), change, evidence examined (commands), verification result,
   missing evidence (for example "no DB spans: Npgsql not instrumented").

| Symptom | Likely cause | Evidence |
|---|---|---|
| Project `Unhealthy`, request works | `/health` not mapped in its environment, or wrong health path | Process environment, `MapDefaultEndpoints` code |
| Project waits forever | Dependency never healthy, or initializer exited non-zero | `aspire logs <res>`: "Waiting for resource"; `describe` exit code |
| Connection refused / wrong host | Code reads a different key than Aspire injects, or a later configuration source overrides it | Env names vs `GetConnectionString("…")`, configuration order |
| AppHost fails at start: local JWT key missing | `devtoken init` not run on this machine | AppHost error message |
| 401 with a devtoken token | Issuer/audience/claims in `devtoken.json` differ from the trust mapping, wrong profile for that API, or key rotated without AppHost restart | `aspire describe <api>` env names; decode the token header/payload (never paste it) |
| 403 with a devtoken token | Token accepted; role/scope claim type or value does not match the policy | `devtoken list`, the policy code |
| Requests slow on first call, warnings `IDX20803` | `Authority` not cleared: JwtBearer tries to download metadata | Trust mapping (`TrustValue.Empty`), `aspire otel traces` client spans |
| Data missing after restart | Expected: every start rebuilds schema + seed | Add what the scenario needs to `local/database/seed/` |
| Initializer failed on seed | Seed script doesn't match the current schema | `aspire logs <initializer>`; fix the script, not the migration |
| No telemetry | `OTEL_EXPORTER_OTLP_ENDPOINT` missing, or service lacks `AddServiceDefaults` | Env names, `aspire otel logs` empty |
| `aspire add` exit code 5 | Non-exact integration name in non-interactive mode | `aspire integration search` |
