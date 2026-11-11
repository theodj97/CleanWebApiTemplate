# Quality gates

Every command an agent needs to prove a change, in the order the repository runs them. Report real results;
a warning is a failure.

## Commands

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx
dotnet format whitespace CleanWebApiTemplate.slnx --verify-no-changes
dotnet format style CleanWebApiTemplate.slnx --verify-no-changes
dotnet format analyzers CleanWebApiTemplate.slnx --verify-no-changes --exclude-diagnostics IL2026 IL3050
dotnet list CleanWebApiTemplate.slnx package --vulnerable --include-transitive
npm run verify
npm run coverage
```

| npm script | What it runs |
|---|---|
| `npm run verify` | `scripts/pre-push.sh`: build, format (whitespace + style), analyzers, every test (`--no-build`) and the vulnerable-package check. Same as the `pre-push` hook. |
| `npm run coverage` | `scripts/coverage.sh`: `dotnet tool restore`, build, coverage for unit/component/functional, merge, report and thresholds. |
| `npm run format` | `dotnet format whitespace` + `dotnet format style` over the solution (writes). |
| `npm run format:check` | The same two commands with `--verify-no-changes`. |
| `npm run test` | `dotnet test --solution`. |
| `npm run hooks:install` | `dotnet tool restore && dotnet husky install`. |
| `npm run gitleaks:install` | `scripts/install-gitleaks.sh`. |

## Build

`TreatWarningsAsErrors` is repository-wide and `AnalysisLevel=latest`/`AnalysisMode=Recommended` applies to
`src/`. A warning in `src/` or in the tests fails the build. `dotnet format analyzers` is run separately
because IDE analyzers and the compiler do not report the same set.

On Windows use `win-x64` in the publish RID. The publish is the AOT gate; the build is not.

## Format

`.editorconfig` is authoritative: file-scoped namespaces, `System` usings first, 4 spaces for C#, 2 for
JSON/XML/YAML, LF everywhere, final newline, no trailing whitespace outside Markdown. IDE0005 (unnecessary
using) is a warning and IDE0060 (unused parameter) is an error; `EnforceCodeStyleInBuild` makes both part of
the build. If a signature needs the parameter, name it `_` or `_1`.

The pre-commit hook runs `dotnet format` on the staged `.cs` only, excludes IDE0060 from the fix pass (a code
fix must not delete a signature parameter) and then verifies IDE0060 with `--verify-no-changes`. It also fails
when a staged file has unstaged edits, because the commit would carry the unformatted version.

## Tests

```bash
dotnet test --solution CleanWebApiTemplate.slnx
dotnet test --project tests/CleanWebApiTemplate.UnitTests/CleanWebApiTemplate.UnitTests.csproj
dotnet test --project tests/CleanWebApiTemplate.Api.ComponentTests/CleanWebApiTemplate.Api.ComponentTests.csproj
```

`global.json` selects Microsoft Testing Platform, so `--solution`/`--project` is required; a positional
project path is rejected. Component and functional tests need Docker (or Podman with
`ASPIRE_CONTAINER_RUNTIME=podman`). Unit and architecture tests need neither.

If Docker is unavailable, say exactly which projects could not run. Never report a suite as passed without
running it.

## Coverage

`npm run coverage` writes `coverage-report/coverage/report/` (`Summary.txt`, `SummaryGithub.md`,
`Cobertura.xml`) and fails below:

| Scope | Lines | Branches |
|---|---:|---:|
| `src/` total | 90 % | 80 % |
| Api | 85 % | 75 % |
| Application | 95 % | 90 % |
| Domain | 100 % | 100 % |
| Infrastructure | 90 % | 85 % |
| ServiceDefaults | 95 % | 100 % |

Only `src/` assemblies are measured; architecture and sidecar tests are not collected. `coverage-report/` is
ignored by Git.

## Vulnerable packages

`scripts/check-vulnerable-packages.sh` runs `dotnet list package --vulnerable --include-transitive` and fails
when the JSON contains a `vulnerabilities` entry. `dotnet list` exits 0 even with findings, so the exit code is
not the gate. Update the package; do not pin an advisory away.

## Hooks

| Hook | Runs |
|---|---|
| `pre-commit` | gitleaks on the staged diff + `dotnet format` on the staged `.cs`. |
| `commit-msg` | `scripts/commit-msg.sh`: the conventional subject. |
| `pre-push` | `scripts/pre-push.sh`: the full gates. |

Husky.Net runs the groups defined in `.husky/task-runner.json`; the hooks themselves only call
`dotnet husky run --group <name>`. `src/Directory.Build.props` sets `AnalysisLevel=latest` and
`AnalysisMode=Recommended` for production code only: tests keep the SDK defaults so test naming and
disposal rules do not hide real signals.

gitleaks is downloaded on first use into `.tools/gitleaks` (`scripts/install-gitleaks.sh`, pinned version and
SHA-256). Fake keys in tests carry `gitleaks:allow` on the line. Skip a hook with `HUSKY=0 git …` or
`--no-verify`, and say so in the report.

## Commit convention

`<type>(<optional scope>): <description>`, 1–72 characters, types `feat`, `fix`, `refactor`, `docs`, `test`,
`chore`, `build`, `perf`, `revert`. The hook also accepts `Merge`, `Revert`, `fixup!` and `squash!`. The body
explains why.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Specifying a project for 'dotnet test' should be via '--project'` | The MTP runner is active: use `--project`/`--solution`. |
| Docker/Podman not found | Start the runtime; set `ASPIRE_CONTAINER_RUNTIME=podman` when needed. |
| Component/functional fixture times out on the first run | Image pull; run again or raise `StartupTimeout`. |
| `'db-init' ended … exit code 1` | Schema error: `aspire logs db-init`; reproduce locally. |
| `'openbao-init' ended … exit code 1` | Secret write failed: `aspire logs openbao-init`. |
| `The application did not read 'API_KEY'` | The initializer did not run or `OpenBao:Path` differs. |
| `Blocked outbound HTTP call to …` | A dependency points at a real URL; map it to a local substitute. |
| `JwtBearer scheme '…' would download metadata` | The trust mapping did not clear `Authority`/`MetadataAddress`. |
| Coverage threshold failure | The report names the assembly and file; add the missing test, not a constant test. |

The template repository also has smoke tests for `dotnet new` and for the NuGet package. They are maintainer
tooling and are not shipped with a generated project.
