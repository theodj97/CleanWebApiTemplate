# Validation, acceptance checklist and final report

Canonical for SKILL.md rule 12 and Phases 11–13. Run every step the environment allows, keep the exit codes, and report
blocked steps with the reason (no container runtime, missing SDK, no network, missing external credentials, policy).
Capture the exit code explicitly when output is filtered: `cmd > out.log 2>&1; echo "exit=$?"`. Never claim a step
passed unless you ran it and saw it pass.

## Contents
- [1. Structure and solution](#1-structure-and-solution)
- [2. Restore, build, tests](#2-restore-build-tests)
- [3. Local environment](#3-local-environment)
- [4. Database](#4-database)
- [5. Smoke requests, tokens and telemetry](#5-smoke-requests-tokens-and-telemetry)
- [6. Authentication and production isolation](#6-authentication-and-production-isolation)
- [7. Agent integration](#7-agent-integration)
- [8. Clean-up](#8-clean-up)
- [Acceptance checklist](#acceptance-checklist)
- [Report table](#report-table)
- [Final report](#final-report)
- [Summary of incremental work](#summary-of-incremental-work)

## 1. Structure and solution

```bash
dotnet sln <solution> list                                          # every new csproj listed
git ls-files --others --exclude-standard -- '*.csproj'              # new projects not yet tracked: all must be listed above
grep -rn --include='*.csproj' -E '(local|tests)[/\\]' src           # must print nothing: src depends on neither
find . \( -name '*.sln' -o -name '*.slnx' \) -not -path '*/bin/*' -not -path '*/obj/*'   # no new solution files
git ls-files '*.csproj' | xargs grep -l 'Aspire.AppHost.Sdk'        # exactly one AppHost, under local/aspire/
```

Also check: the AppHost and local-only projects live under `local/aspire/`, tools under `local/tools/`, tests under
`tests/` with no orchestration project; every new project and folder follows `{App}.<Role>`; ServiceDefaults (if any)
is in `src/`; relative `ProjectReference` paths resolve; component/functional projects reference the AppHost with
`Aliases="apphost"`; CPM is respected (no `Version=` on `PackageReference` when CPM is on); no empty folders;
`local/` is not git-ignored.

## 2. Restore, build, tests

```bash
dotnet restore <solution>
dotnet build <solution> --no-restore            # no errors; no new warnings when warnings are errors
dotnet test <solution> --no-build               # or the repository's documented command/filters
```

- Unit tests pass without Docker; component and functional tests run with Docker/Podman. Without a runtime, say
  exactly which projects could not run.
- Report failures that existed before your change separately (baseline run after the gap report, or a clean checkout
  if cheap and safe; otherwise say you could not tell).
- **Prove that at least one new test can fail**: change its expected value, run it, see it fail, revert, run it again.
- Run the full suite at least twice when the infrastructure is new: flaky startup or reset ordering shows up there.

## 3. Local environment

Needs a container runtime.

```bash
aspire ps --format Json                                             # AppHosts already running: don't touch them
dotnet local/tools/devtoken.cs init                                 # once per machine (idempotent)
aspire start --apphost <AppHost.csproj> --format Json               # redact the dashboardUrl token
aspire wait <each key resource> --apphost <AppHost.csproj> --timeout 300
aspire describe --apphost <AppHost.csproj> --format Json            # summarize name/state/health/exitCode only
```

Expected: infrastructure `Running` and `Healthy`; the initializer `Finished` with exit code 0; applications `Running`,
and `Healthy` when they have health checks. Explain any other state with logs.

## 4. Database

- The initializer's logs list the applied migrations or schema scripts, then each seed script
  (`aspire logs <initializer>`).
- No data resource declares a volume, bind mount or persistent lifetime (check `AppHost.cs`; `aspire describe` shows no
  volumes).
- A smoke request reads the seed data and writes a new record.
- **Two-run check**: write a record in run 1; `aspire stop --apphost <csproj>`; `aspire start` + `aspire wait`; the
  record is gone and the seed data is back.
- After the final stop and a few seconds of asynchronous clean-up, `docker volume ls` / `docker ps -a` show nothing left
  by the AppHost.
- Tests: the initializer finished before the fixture created resetters (fixture startup log or a passing run).

## 5. Smoke requests, tokens and telemetry

- Take endpoints from `describe` `urls`; never guess ports.
- For each protected service: `TOKEN=$(dotnet local/tools/devtoken.cs get --profile <allowed>)` → success;
  no token → 401; an insufficient-permission profile → 403; a token for another API (audience) → 401; a token with
  one character changed → 401.
- An application with its own login endpoint: the login still works in Local mode and its token is accepted.
- Call a route that touches each key dependency, then follow `observability.md` § Verification: matching trace,
  structured logs, dependency spans (or an explicit "not instrumented"), cross-service correlation when services call
  each other.

## 6. Authentication and production isolation

| Check | How |
|---|---|
| No identity provider contacted in Local mode | `aspire otel traces <api>` after authenticated requests: no client span to a provider; trust mapping env names present (`aspire describe`, values redacted) |
| No identity provider contacted in tests | component fixture guard passes; functional valid-token tests assert an empty `OutboundCallRecorder` |
| The guards work | remove one `TrustValue.Empty` mapping on purpose: the component fixture must fail naming the metadata address and the functional recorder must list the `CONNECT`; then revert and rerun |
| Validation is real | the authentication matrix passes (`local-jwt-authentication.md` § Authentication and authorization tests): invalid, expired, wrong issuer/audience, unknown key and unsigned tokens get 401, insufficient permissions 403 |
| Every protected service treated the same | each one has a trust mapping, devtoken profiles and the matrix, whether or not it has login endpoints |
| No local/test values in production configuration | `grep -rn -E '<local issuer>|<test issuer>|local-dev|:tests' src` prints nothing; no keys in committed `*.json` under `src/` or `local/` |
| Nothing local deployable | `src/` references neither `local/` nor `tests/`; devtoken, AppHost and DbInitializer are not in publish/deploy pipelines |
| No bypass | no `AllowAnonymous`/test scheme/`ValidateX = false` introduced; policies unchanged (`git diff` of authorization code is empty unless the user approved a change) |
| Secrets | `git status`/`git diff` contain no token, key, password, connection string with credentials or dashboard token; local keys are only in the AppHost user secrets |

## 7. Agent integration

- `aspire agent init --help` supports the options used; installed `SKILL.md` files are at the repository-root
  location(s); no unintended `.mcp.json`; no nested duplicates; custom skills unchanged.

## 8. Clean-up

- Stop only the AppHost you started (`aspire stop --apphost <csproj>`); its session containers are removed with it.
  Never remove pre-existing volumes or containers; report orphaned ones by name.
- Remove temporary edits used for verification (sabotaged expectations, removed mappings), rebuild and rerun what they
  touched.
- Undo machine-level side effects you caused without consent (global npm packages, template installs, certificates),
  or report them. A local JWT key created by `devtoken init` is intended and stays.

## Acceptance checklist

The work is complete only when each item is satisfied or explicitly reported as blocked with the reason.

**Skill rules and structure**
- [ ] `src/` holds application code only; `tests/` holds tests only; `local/` holds the AppHost, local workers, seed,
      tools, scripts and `local-development.md`; nothing in `src/` references `local/` or `tests/`.
- [ ] Every created project and folder is named `{App}.<Role>`; none uses a generic template name.
- [ ] Every new project appears in `dotnet sln <existing solution> list`; no new solution file exists.
- [ ] Existing architecture, migration history, names and user changes are preserved; production code changes are
      only the declared testability ones or explicitly approved fixes.

**One AppHost**
- [ ] Exactly one AppHost, under `local/aspire/`, used by `aspire run` and by every component/functional fixture
      (`--RunMode`); any former test AppHost was consolidated and removed (with consent) or is reported as pending.
- [ ] Mode differences are configuration and additions; the topology is declared once.
- [ ] Every resource is justified by inventory evidence; references and readiness waits are explicit.

**Data**
- [ ] Each database has one authoritative schema strategy, applied by the initializer (or the app) before dependants,
      in every mode.
- [ ] No data volume, bind mount or persistent lifetime anywhere; the two-run check passed; seed data lives in
      `local/database/seed/*.sql` and runs only in Local mode; pre-existing volumes and containers were not deleted.

**Local development**
- [ ] The AppHost started, resources reached the expected state, a smoke request succeeded and its trace/logs were
      found with `aspire otel`/`aspire logs`.
- [ ] `aspire agent init` ran with verified options (MCP only with consent), or the blocker is reported.
- [ ] `local/local-development.md` describes the real implementation, including tokens and tests.

**Authentication**
- [ ] Every protected service runs and is tested with locally signed JWTs through its trust mapping, independently of
      login endpoints; no token comes from a login endpoint or an external provider.
- [ ] Tokens are generated without any endpoint (`devtoken`, `TestTokenIssuer`); applications validate them without
      downloading metadata or keys (guards and recorder pass, sabotage detected), except a strategy-C loopback stub the
      user approved, which never leaves the machine.
- [ ] Roles, claims, scopes and policies match the real configuration; the authentication matrix passes over real
      HTTP requests (component pipeline and functional processes).
- [ ] No bypass, no accept-any-token, no unsigned tokens accepted, no local key or issuer enabled for production; no
      secret committed.

**Tests**
- [ ] Test projects follow the unit/component/functional split, AAA, naming, identity, reset-before and determinism
      rules; component/functional projects run serially and use the shared AppHost's dynamic ports.
- [ ] Solution builds; tests ran with their real results; one new test was shown to fail and was reverted.

## Report table

```text
| Step | Command | Exit | Result |
|------|---------|------|--------|
| Solution inclusion | dotnet sln X.slnx list | 0 | AppHost, DbInitializer, ServiceDefaults, 5 test projects listed |
| Build | dotnet build X.slnx | 0 | 0 warnings, 0 errors |
| Tests | dotnet test X.slnx | 0 | 52 passed (5 unit, 29 component, 18 functional) |
| Fail proof | dotnet test --filter … (expected value changed) | 1 | failed as expected; reverted, passes |
| Local key | dotnet local/tools/devtoken.cs init | 0 | key in AppHost user secrets |
| Start | aspire start --apphost … | 0 | started |
| Wait | aspire wait orders-api … | 0 | healthy |
| DB init | aspire logs db-init | 0 | 1 migration + 2 seed scripts applied |
| Two-run | stop/start + GET /orders | 200 | run-1 changes gone, seed rows present |
| Auth smoke | curl with devtoken profiles | — | 200 / 401 / 403 as expected per profile |
| No IdP | aspire otel traces catalog-api | 0 | server spans only |
| Smoke + trace | curl GET /orders | 200 | trace with server + PostgreSQL client spans |
| Agent init | aspire agent init … | 0 | 4 skills in .claude/skills |
```

## Final report

1. Tooling versions used and verified (.NET SDK, Aspire CLI and packages, container runtime), also written to
   `local/local-development.md`; files created and modified; resulting structure and AppHost path; projects added to
   the solution (`dotnet sln list` evidence).
2. Dependency inventory; database strategy per database and why; AppHost modes and what differs per mode.
3. Trust strategy per protected service (A–D), algorithm and why; devtoken profiles; how tests obtain tokens.
4. Observability changes and what was verified; agent integration command, skills, locations, MCP decision.
5. Validation commands with exit codes (report table), including the fail proof and the guard sabotage.
6. Decisions, assumptions, production-code changes and why, findings (bugs, bypasses, committed secrets, missing
   `TimeProvider`), and pending items or blockers with what the user must do.

## Summary of incremental work

At the end of incremental or partial-setup test work (former testing skill, kept):

1. What was created or changed: test classes, builders, repositories, `TestUser` values, contract keys, and any
   production-code change with its reason.
2. Decisions, including the authentication used by the new tests and any test type chosen for a behaviour.
3. Assumptions (inferred schema, environment name, wire formats, event shapes, provider contracts).
4. Pending items: questions, tests not run and why, improvements found (missing `TimeProvider`), bugs found (with the
   failing test).

Plus the build and test commands with their real results and the fail proof (one new test failed after changing its
expected value, then passed after reverting).
