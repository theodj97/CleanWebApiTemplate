# Quality gates

A change is done when the gates are green. Report real commands and real exit codes; never claim a gate passed
without running it.

## The commands

```bash
# Build the solution: zero warnings, zero errors
dotnet build {App}.slnx

# Native AOT publish: the real AOT gate
dotnet publish src/{App}.Api/{App}.Api.csproj -c Release -r linux-x64 --self-contained

# Every test (unit, component, functional, architecture); Docker for component/functional
dotnet test --solution {App}.slnx

# Same gates as the pre-push hook
npm run verify

# Coverage report + thresholds
npm run coverage
```

On Windows use `win-x64` for the publish RID. On Apple Silicon use `linux-arm64`/`osx-arm64` as appropriate,
or a container. The publish must target a real RID; `dotnet publish` without one is not the AOT gate.

## What `npm run verify` runs

`scripts/pre-push.sh`:

1. `dotnet build {App}.slnx`
2. `dotnet format whitespace {App}.slnx --verify-no-changes --no-restore`
3. `dotnet format style {App}.slnx --verify-no-changes --no-restore`
4. `dotnet format analyzers {App}.slnx --verify-no-changes --no-restore --exclude-diagnostics IL2026 IL3050`
5. `dotnet test --solution {App}.slnx --no-build`
6. `scripts/check-vulnerable-packages.sh`

The `IL2026`/`IL3050` exclusion is a known false positive of `dotnet format analyzers` for minimal APIs; the
`dotnet publish` gate covers those warnings. Do not add suppressions to make the analyzer step pass.

## Format and analyzers

```bash
npm run format          # dotnet format whitespace + style
npm run format:check    # verify no changes
```

- The repository uses `.editorconfig` and `EnforceCodeStyleInBuild`; the build treats warnings as errors.
- `dotnet format` writes the file; the pre-commit hook formats staged `.cs` files and fails if it changed
  anything, so review and re-stage.
- Unused usings (`IDE0005`) are enforced by `dotnet format`; the API generates XML docs so the rule also runs
  in build there.

## Coverage

`npm run coverage` runs the unit, component and functional projects with `dotnet-coverage`, merges them,
writes `coverage-report/coverage/report/` (`Summary.txt`, `SummaryGithub.md`, `Cobertura.xml`) and fails when a
threshold is not met.

| Scope | Lines | Branches |
|---|---:|---:|
| `src/` total | 90% | 80% |
| `Api` | 85% | 75% |
| `Application` | 95% | 90% |
| `Domain` | 100% | 100% |
| `Infrastructure` | 90% | 85% |
| `ServiceDefaults` | 95% | 100% |

Only `src/` assemblies are measured. Do not lower a threshold to land a feature; add the missing tests.
Architecture tests do not add meaningful coverage of `src/`; component/functional tests do.

## Hooks

| Hook | Runs |
|---|---|
| `pre-commit` | gitleaks on staged changes; `dotnet format` on staged `.cs` files (fails if it changed anything); IDE0060 check on the staged files |
| `commit-msg` | Conventional subject: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`, `perf`, `revert` |
| `pre-push` | `npm run verify` (build, format, analyzers, all tests, vulnerable packages) |

Install with:

```bash
npm run hooks:install     # dotnet tool restore && dotnet husky install
```

gitleaks is downloaded on the first `pre-commit` by `scripts/install-gitleaks.sh` (pinned version and SHA-256).
Skip hooks only when the user asks: `HUSKY=0 git ...` or `--no-verify`. Agents do not commit or push unless
asked.

## Commit message

```text
<type>(<optional-scope>): <description of 1-72 characters>
```

The hook allows `Merge ...`, `Revert ...`, `fixup! ...` and `squash! ...`. Scope is lowercase and hyphenated.
The body goes after a blank line. Do not add generated-by trailers unless the user asks.

## Vulnerable packages

`scripts/check-vulnerable-packages.sh` runs
`dotnet list <solution> package --vulnerable --include-transitive` (table + JSON) and fails on any finding,
including transitive ones. Fix by upgrading the package (and checking AOT compatibility) rather than
suppressing the advisory. `Directory.Build.props` also enables `NuGetAudit`/`NuGetAuditMode=all` at restore
time.

## Template smoke (template repository only)

`bash scripts/template-smoke.sh` installs the template in an isolated hive, generates two projects, checks the
output (no `.agents/`, `.tools/`, audit or reports; distinct `UserSecretsId`; executable hooks) and builds,
tests and AOT-publishes the generated API. Run it when the template itself changes, not in a generated
project.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Specifying a project for 'dotnet test' should be via '--project'` | MTP is active (`global.json`): use `--project` or `--solution` |
| Build fails with a style warning | `EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`; run `npm run format` and fix the cause |
| `dotnet format` changed files in pre-commit | The hook fails on purpose; review and re-stage |
| Analyzer reports `IL2026`/`IL3050` but publish is clean | Known `dotnet format analyzers` false positive; the publish is the AOT gate |
| Publish fails with a missing RID/toolchain | Install the AOT prerequisites for the RID (clang, zlib, etc.) or publish in the supported container |
| Tests fail to start containers | Start Docker; for Podman set `ASPIRE_CONTAINER_RUNTIME=podman` |
| First component/functional run is slow | Images are being pulled; the fixture allows five minutes |
| `db-init` exited 1 | Read `aspire logs db-init`; a SQL script failed or the readiness query references a missing table |
| `openbao-init` exited 1 | Read `aspire logs openbao-init`; a required `OpenBao__*` value or the secret is missing |
| `CS0433: The type 'Program' exists in both` | Reference the AppHost with `Aliases="apphost"` and use `extern alias apphost;` |
| Coverage threshold fails after a feature | Add unit/component/functional tests; do not lower the threshold |
| gitleaks blocks a commit | Remove the secret from the staged change and rotate it; never bypass with `--no-verify` unless the user asks |

## Definition of done

- [ ] `dotnet build` — 0 warnings, 0 errors.
- [ ] `dotnet publish -c Release -r <rid> --self-contained` — 0 warnings.
- [ ] `dotnet test --solution` — all pass (or the exact projects that could not run are reported).
- [ ] `npm run verify` — all gates pass.
- [ ] `npm run coverage` — all thresholds pass.
- [ ] No secret, token, key or dashboard URL in the diff or the report.
- [ ] Docs updated (README API table, tests/local docs when their contracts change).
- [ ] One new test was shown to fail without the change.
