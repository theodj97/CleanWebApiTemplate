# Validation

Run every step the environment allows, keep the exit codes, and report blocked steps with the reason (no container
runtime, missing SDK, no network, missing external credentials, policy). Capture the exit code explicitly when
output is filtered: `cmd > out.log 2>&1; echo "exit=$?"`.

## 1. Structure and solution (always)

```bash
dotnet sln <solution> list                                  # every new csproj listed
git ls-files --others --exclude-standard -- '*.csproj'      # new projects not yet tracked: all of them must be in the list above
grep -rn --include='*.csproj' -E 'local[/\\]' src           # must print nothing: src must not depend on local/
find . -name '*.sln' -o -name '*.slnx' | grep -v -E '/(bin|obj)/'   # no new solution files
```

Also check:

- the AppHost and local-only projects live under `local/aspire/`;
- every new project and its folder follow `{App}.<Role>` (for example `local/aspire/{App}.AppHost/{App}.AppHost.csproj`);
- ServiceDefaults (if any) lives under `src/`;
- relative `ProjectReference` paths resolve;
- CPM is respected: no `Version=` on `PackageReference` when CPM is on.

## 2. Restore, build, tests

```bash
dotnet restore <solution>
dotnet build <solution> --no-restore
dotnet test <solution> --no-build        # or the repository's documented test command/filters
```

Report failures that existed before your change separately. Check them against `git stash`/a clean checkout only if
it is cheap and safe; otherwise say you could not tell.

## 3. Environment (needs a container runtime)

```bash
aspire ps --format Json                                         # note AppHosts already running; don't touch them
aspire start --apphost <AppHost.csproj> --format Json           # redact dashboardUrl token
aspire wait <each key resource> --apphost <AppHost.csproj> --timeout 300
aspire describe --apphost <AppHost.csproj> --format Json        # summarize name/state/health/exitCode only
```

Expected:

- infrastructure resources are `Running` and `Healthy`;
- initializers are `Finished` with exit code 0;
- application projects are `Running`, and `Healthy` when they have health checks.

Explain any other state with logs.

## 4. Database

- Initializer logs list the applied migrations or scripts (`aspire logs <initializer>`).
- Fresh-database run (see `database.md` § Fresh-database verification) when safe.
- A smoke request that writes and reads data through the application.
- Restart check when persistence matters: stop and start the AppHost (plain stop), and the data is still there.

## 5. Smoke test and telemetry

- Take the endpoint from `describe` `urls`; never guess the port.
- Call a real route, ideally one that touches each key dependency.
- Then follow `observability.md` § Verification: matching trace, structured logs, dependency spans (or an explicit
  "not instrumented"), cross-service correlation when there are service-to-service calls.

## 6. Agent integration

- `aspire agent init --help` supports the options you used.
- Installed `SKILL.md` files are at the repository-root skill location(s).
- No unintended `.mcp.json`, no nested duplicates, and custom skills unchanged.

## 7. Clean-up

- Stop only the AppHost you started: `aspire stop --apphost <csproj>`. Leave persistent containers and volumes
  unless they are temporary verification artifacts you created; remove those individually, by name.
- Remove temporary edits used for verification (for example a temporary database name), then rebuild.
- Undo machine-level side effects you caused without consent (global npm packages, template installs), or report
  them.

## Report table

```text
| Step | Command | Exit | Result |
|------|---------|------|--------|
| Solution inclusion | dotnet sln X.slnx list | 0 | AppHost, ServiceDefaults listed |
| Build | dotnet build X.slnx | 0 | 0 warnings, 0 errors |
| Tests | dotnet test X.slnx | 1 | 2 failing (pre-existing: …) |
| Start | aspire start --apphost … | 0 | started |
| Wait api | aspire wait api … | 0 | healthy |
| Fresh DB | … | 0 | 3 migrations applied |
| Smoke | curl POST /orders | 201 | trace 6c86… with server span; no DB span (not instrumented) |
| Agent init | aspire agent init … | 0 | 4 skills in .claude/skills |
```
