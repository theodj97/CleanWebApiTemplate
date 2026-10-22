# Official Aspire AI agent integration and debugging loop

Official guidance: <https://aspire.dev/get-started/ai-coding-agents/>, <https://aspire.dev/get-started/aspire-skills/>,
<https://aspire.dev/dashboard/ai-coding-agents/>. Do not rewrite or duplicate the official Aspire skills here. This
skill installs them and uses the CLI evidence they describe.

## Setup

1. **CLI and options.** `aspire --version`, then `aspire agent init --help`. Proceed only if the help lists
   `--skills`, `--skill-locations` and `--workspace-root`. If the CLI is missing or too old, report it and give the
   official install/update instructions from `references/sources.md`. Do not claim the agent setup was done.
2. **Existing installations.** Look for Aspire skills (`aspire`, `aspire-init`, `aspire-orchestration`,
   `aspire-monitoring`, …) in `.claude/skills/`, `.agents/skills/`, `.github/skills/`, `.opencode/skill/`, and in
   nested folders such as `local/aspire/*/.claude/skills/`, plus `.mcp.json`.
   - `aspire agent init` removes its skills from locations you do not select. Include every location the repository
     already uses (`claudecode` → `.claude/skills/`, `standard` → `.agents/skills/`, `github` → `.github/skills/`,
     `opencode` → `.opencode/skill/`), unless the user wants to drop one.
   - A nested installation under the AppHost folder is only discovered when Claude touches that folder. Replace it
     with a root installation, then remove the nested copy only after the root one is verified.
3. **Run from the repository root**, so the skills land next to the repository's other Claude Code skills and not
   under `local/aspire/{App}.AppHost/`:

   ```bash
   aspire agent init --non-interactive --workspace-root . \
     --skills aspire,aspire-init,aspire-orchestration,aspire-monitoring \
     --skill-locations claudecode            # add ,standard when the repo is shared with .agents/skills agents
   ```

   - Use an **explicit skill list**. In 13.6.1, `--skills all` also installs the CLI-provided companions: the
     `dotnet-inspect` skill and the `playwright-cli` skill, and the latter runs a **global npm install of
     `@playwright/cli`**. Add companions (`dotnet-inspect`, `playwright-cli`) only when the user wants them and
     accepts that global install.
   - Add `aspire-deployment`, `aspireify` or `aspire-project-v2-migration` when the user's workflow needs them. The
     official docs recommend installing the workflow bundle together; local development needs at least the four
     above.
   - Skill names come from the bundle and may change between releases. If the CLI rejects a name, list what the
     installed CLI offers (run it interactively, or read the error) and adjust.
4. **Verify.**
   - `ls .claude/skills/*/SKILL.md` shows the installed skills next to the existing custom ones, and each has
     `name`/`description` frontmatter.
   - Custom skills (including this one) are untouched: compare against `git status`.
   - Report the exact command, the installed skills and locations, and whether `.mcp.json` was created.
   - Claude Code picks up skills in an existing `.claude/skills/` live. If the directory was new in this session,
     `/reload-skills` or a new session is needed, so say which.
5. **Commit guidance.** The installed skill files are repository content. Tell the user they can be committed, or
   ignored if the team prefers each developer to run `aspire agent init`. Follow existing repository practice.

## MCP (optional)

The Aspire CLI already exposes resources, logs, traces and restarts, so MCP is **not** required. Configure it only when
the user wants agents to use MCP tools (for example an agent host without shell access), and they opt in:

```bash
aspire agent init --non-interactive --workspace-root . --skills none --skill-locations claudecode --mcp
```

For Claude Code this writes `.mcp.json` with `{"mcpServers":{"aspire":{"command":"aspire","args":["agent","mcp"]}}}`.
Security model from the official docs:

- STDIO only, no network port.
- It does not expose source code, environment variable values or secrets, but resource logs can still contain
  sensitive data.
- Hide resources with `.ExcludeFromMcp()` in the AppHost when needed.

Never pass `--mcp` silently. Pass `--mcp=false` to state the opt-out explicitly when re-running init.

## Debugging loop

Use this loop for `debug <symptom>` and whenever validation fails. Evidence first, then the smallest fix, then the
same scenario again.

1. **Inspect, don't duplicate.**

   ```bash
   aspire ps --format Json                                   # is the AppHost already running?
   aspire describe --apphost <csproj> --format Json          # states, health, exit codes, urls, relationships
   aspire logs <resource> --apphost <csproj> --tail 100      # console output incl. "Waiting for resource …"
   aspire otel logs <resource> --apphost <csproj> --format Json --severity Warning
   aspire otel traces <resource> --apphost <csproj> --format Json --limit 20 [--has-error true]
   ```

   Redact `environment` values and any `dashboardUrl` token before quoting output.
2. **Start or wait only when needed.**
   - `aspire start --apphost <csproj>` (background) only if not running. In a second worktree or for a parallel run,
     add `--isolated`.
   - Then `aspire wait <resource> --status healthy|up --timeout <s>`.
   - Exit codes: 0 means the resource reached the status. A non-zero code means it did not, so read
     `describe`/`logs` before retrying.
3. **Locate the failure.**
   - Which resource is `FailedToStart`, `Exited` (exit code), `Unhealthy`, or stuck waiting (its log shows
     `Waiting for resource 'x'`)?
   - For an HTTP failure, reproduce the request, then find its trace:
     `aspire otel traces <res> --search "<route>"`, or `--trace-id <id>` from the response or logs.
   - Correlate logs to that trace: `aspire otel logs <res> --trace-id <id>`.
   - Passing health checks do not prove requests work.
4. **Hypothesis from evidence.** Tie the logs/traces to source code and configuration. Classify the cause:
   application code, configuration or keys, dependency readiness (`WaitFor` missing), schema initialization,
   networking/endpoints, container runtime, certificates.
   - Check the environment variable **names** a process received before blaming the code, for example a missing
     `ConnectionStrings__x`, `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT` or `OTEL_EXPORTER_OTLP_ENDPOINT`.
5. **Fix and verify.**
   - Make the smallest change.
   - Restart only the affected resource (`aspire resource <res> restart`) when the change does not touch the AppHost
     model. Changes to `AppHost.cs` need `aspire stop` + `aspire start` for the AppHost you started (plain stop,
     never `--force --volumes`).
   - Repeat the **original** failing scenario, check the new response, then confirm the new trace/logs show the
     expected behaviour. "Healthy" alone is not verification.
6. **Report honestly.** Cause (confirmed or hypothesis), change, evidence examined (commands), verification
   result, and missing evidence (for example "no DB spans: Npgsql not instrumented").

Typical findings:

| Symptom | Likely cause | Evidence |
|---|---|---|
| Project `Unhealthy`, request works | `/health` not mapped in its environment, or wrong health path | Process environment, `MapDefaultEndpoints` code |
| Project waits forever | Dependency never healthy, or initializer exited non-zero | `aspire logs <res>`: "Waiting for resource"; `describe` exit code |
| Connection refused / wrong host | Code reads a different key than the one Aspire injects, or a configuration source added after environment variables overrides it | Env names vs `GetConnectionString("…")`, configuration setup order |
| Login failed after a while | Volume kept old password, user secrets changed | Container logs, user secrets `Parameters:*-password` |
| No telemetry | `OTEL_EXPORTER_OTLP_ENDPOINT` missing, or service lacks `AddServiceDefaults` | Env names, `aspire otel logs` empty |
| `aspire add` exit code 5 | Non-exact integration name in non-interactive mode | `aspire integration search` |
