---
name: dotnet-aspire
description: >-
  Builds, repairs and validates how a .NET repository runs locally and is tested with Aspire: ONE AppHost under
  local/aspire/ shared by local development (dashboard, telemetry, ephemeral databases rebuilt with schema + seed,
  debugging with Aspire CLI evidence) and by unit, component and functional tests (xUnit, Aspire.Hosting.Testing,
  real containers, WireMock), with locally signed JWTs for every protected API (devtoken tool, TestTokenIssuer) and no
  identity provider. Use it to set up or fix the local environment, run or debug the app locally, get local tokens,
  or add, extend or fix tests of endpoints, roles, flows, classes or the test infrastructure, even if Aspire or xUnit
  are not mentioned: "add tests for POST /orders", "levanta el entorno local", "añade tests", "testea este endpoint",
  "dame un token de admin", "monta la infraestructura de pruebas". Arguments: audit | setup | repair | debug <symptom>.
argument-hint: "[audit | setup | repair | debug <symptom>]"
compatibility: Needs the repository's .NET SDK (10+ for the single-file devtoken tool), the Aspire CLI (verified with 13.6.x) and Docker or Podman for containers. Unit tests need none of them.
---

# .NET with Aspire: one AppHost for local development and tests, local JWT authentication

You turn a .NET repository you have never seen into one that starts locally with a single Aspire command and is tested
against the same topology, with real infrastructure, correctly initialized databases, meaningful telemetry, a clean
`src/` · `tests/` · `local/` split, and authentication that never needs an identity provider. Everything comes from
evidence in the repository, never from a template. This skill supersedes `dotnet-aspire-local-environment` and
`dotnet-structured-testing`; rules marked *new* or *changed* did not exist in them in this form.

*Changed:* the former local-environment skill could only be invoked explicitly (`disable-model-invocation: true`).
This skill is also selected automatically, because test requests arrive in natural language. The safeguard is the same
in every mode that changes structure: present the plan and wait for confirmation before substantial changes.

## Mode

Read `$ARGUMENTS`, or the user's request when the skill was selected automatically:

| Request | Mode | Phases |
|---|---|---|
| `audit` | Discovery and gap report only. No file changes. | 0–2 |
| `setup`, or empty / "set up the environment or tests" with no AppHost | Full creation | 0–13 |
| `repair`, or empty with an existing AppHost (including consolidating a test AppHost) | Reconcile with the rules below | 0–13 |
| `debug <symptom>` | Runtime diagnosis (`references/local-development.md` § Debugging loop) | until a verified fix or an honest report |
| Tests for a behaviour (endpoint, case, flow, class) and the infrastructure exists | Incremental test work | 0, 1 (short report), 9, 11 (build, tests, fail proof), summary; 3 and 8 only for announced additive changes (a new infrastructure dependency) |
| Tests requested but that test type has no project or fixture yet | Partial setup of that piece, announced first, then incremental | 0–2 (short), 8 for the missing piece, 9, 11, summary |
| "Give me a token for …" | `dotnet local/tools/devtoken.cs get --profile <name>` if devtoken exists; otherwise explain and propose `setup`/`repair` | — |

## Non-negotiable rules

Each rule is defined here and detailed in exactly one reference (the canonical place for its details).

1. **Folder responsibilities and names.** `src/` holds application and shared runtime code for every environment;
   `tests/` holds every automated test and nothing that orchestrates; `local/` holds everything local-only: the
   AppHost and local workers under `local/aspire/`, `local/database/`, `local/tools/`, `local/scripts/`,
   `local/local-development.md`. Every project you create is named `{App}.<Role>` with a folder of the same name, never
   a generic template name. This is a repository convention, not an Aspire requirement: never restructure existing
   application code to fit it. → `references/architecture-and-layout.md`
2. **Nothing deployable depends on `local/` or `tests/`.** Projects in `src/` build and deploy without them. Code the
   applications need at runtime (ServiceDefaults) is shared runtime code in `src/`. Test projects may reference the
   AppHost in `local/` (they are never deployed). → `references/architecture-and-layout.md` § Project references
3. **One solution.** Discover the authoritative `.sln`/`.slnx` first; add every new project to it, respecting existing
   solution folders. Never create a second solution, never convert between formats.
   → `references/architecture-and-layout.md` § Adding projects to the solution
4. **One AppHost, three modes** (*new; replaces the former "dedicated test AppHost" rule*). A single AppHost at
   `local/aspire/{App}.AppHost/` is the source of truth for the topology. `aspire run` uses Local mode; component and
   functional fixtures start the same AppHost with `--RunMode=Component|Functional` through
   `DistributedApplicationTestingBuilder`. Modes differ only through configuration and additions, never through a
   copied topology, and may use different values where isolation requires it. A second AppHost for tests is a gap to
   consolidate. → `references/apphost.md`
5. **Evidence over assumption.** A dependency, role, claim or configuration key exists when code, configuration or
   infrastructure files use it, not because a package is referenced or a template suggests it. Every API, CLI option
   and package version is verified against the installed tooling (`<command> --help`, `aspire docs api search`,
   `aspire integration search`, NuGet) before use; never copy examples from incompatible versions or invent members.
   → `references/discovery.md`
6. **Minimal, reversible changes; production code is not yours to change.** Preserve architecture, names, namespaces,
   migration history, existing infrastructure and user modifications. No unrelated upgrades or reorganizations; never
   delete and recreate an existing AppHost because it differs from a template; follow existing test conventions in
   incremental work. Production code changes are limited to declared testability changes (`public partial class
   Program;` on .NET 8/9, `InternalsVisibleTo`). Never change production code or authorization policies to make a test
   or a local run pass: report the bug with the failing test. A rule violation found in production code is reported
   with evidence and a proposed fix, and fixed after consent; violations in `local/` or `tests/` are fixed directly as
   part of the confirmed plan, except moving, renaming or removing projects (an AppHost, a test AppHost), which needs
   explicit consent.
   → `references/discovery.md` § 9
7. **Ephemeral by design: every start, in every mode, begins from scratch.** No `WithDataVolume`,
   `WithDataBindMount`, `WithLifetime(ContainerLifetime.Persistent)` or `WithPersistentLifetime` on local data
   resources: no opt-in, no exceptions. Each database is built in a fixed order on every start: create, schema, seed
   (Local mode only, synthetic SQL in `local/database/seed/`), then the applications (`WaitForCompletion`). Never delete
   pre-existing volumes or containers, never run `aspire stop --force --volumes` or `docker volume rm` without explicit
   consent for that operation, never connect to shared or production databases. → `references/database.md`
8. **Real infrastructure, simulated third parties, nothing leaves the machine in tests.** Databases, caches, brokers
   and storage run as containers or official emulators with the same engine as production; never SQLite, EF InMemory
   or in-memory brokers as substitutes. Only third-party HTTP/gRPC APIs are simulated (WireMock, test modes only;
   the strategy-C identity stub of rule 9 is the single, consented exception). Every real URL is overridden in tests and safety nets make a missed one fail fast and visible.
   → `references/external-services.md`
9. **Local authentication never depends on an identity provider** (*new*). Every protected service (HTTP, gRPC,
   SignalR), with or without login endpoints, runs locally and is tested with JWTs **signed locally** (devtoken for
   development, `TestTokenIssuer` for tests) and validated with explicit keys and parameters: no external login, remote
   metadata, remote JWKS, token exchange, personal credentials, copied tokens, production secrets or Internet access.
   Login endpoints are never used to obtain tokens for local development or for other tests; they are tested as
   ordinary endpoints. Tokens are always signed; signature, issuer, audience and lifetime validation stay on; no bypass,
   no accept-any-token scheme, no policy changes. Keys of local development and tests are isolated from each other and
   from production, and so are issuers and audiences whenever the application reads them from configuration (values
   pinned in code are kept and reported); only the AppHost or the fixtures supply them. Asymmetric keys whenever the application accepts them through configuration; otherwise a per-environment
   symmetric key. A strategy that would need a production change is a question for the user.
   → `references/local-jwt-authentication.md`
10. **Secrets.** Never commit or print passwords, tokens, keys, connection strings or dashboard login URLs, and never
    write them to logs, traces, metrics or exceptions. The only exception is synthetic, local-only passwords of seeded
    test users, documented as such (`references/database.md` § Seed data). `aspire describe --format Json` includes resource environment
    variables and `aspire start`/`aspire ps` return a `dashboardUrl` with a login token: redact them before showing
    output. Local keys live only in the AppHost user secrets; test keys only in memory.
    → `references/local-jwt-authentication.md` § Key storage, `references/observability.md` § Secrets in telemetry
11. **No machine-wide side effects without consent.** Ask before installing global tools, templates or npm packages,
    trusting certificates, or touching user-level agent configuration. (`devtoken init` writing the AppHost user
    secrets is the documented, intended exception.) → `references/apphost.md` § Creating the AppHost,
    `references/local-development.md` § Official Aspire AI agent integration
12. **Truthful reporting and real verification.** Report real commands with real exit codes; never hide an exit code
    behind `tail`/`grep` when it matters. Never claim a build, migration, startup, test, telemetry check or "no external
    call" passed unless you ran it and saw it pass; prove that a new test can fail. Distinguish confirmed findings from
    hypotheses and name every step you could not run. → `references/validation-and-checklist.md`

## Workflow

### Phase 0 — Tooling and workspace state
Run and record the commands of `references/discovery.md` § 0 (git status, SDKs, `global.json`, Aspire CLI, container
runtime, running AppHosts, templates). Read `CLAUDE.md`/`AGENTS.md` and honor them. No container runtime or CLI:
say so immediately and continue with what does not need them.

### Phase 1 — Discovery
Follow `references/discovery.md` (read-only): solution and build, executables and launch profiles, existing AppHosts
(including test AppHosts), dependencies with evidence and classification, configuration keys, databases, **every
protected service's authentication and authorization** (scheme, key source, configurability, claims, roles, scopes,
policies, token issuers), telemetry, existing tests and docs, committed secrets.

### Phase 2 — Gap report and plan
Present the gap report (`references/discovery.md` § Report templates): inventory, gaps against the rules, the plan
(files, projects and solution folders, AppHost modes, database strategy per database, trust strategy A–D per protected
service, test projects) and the decisions that need the user. `audit` ends here. In `setup`/`repair` **wait for
confirmation** before substantial changes; safe independent work (reading, building, running the existing tests as a
baseline) may continue. Incremental test work uses the short report and continues unless something blocks.

### Phase 3 — Layout, AppHost and solution
`references/architecture-and-layout.md` and `references/apphost.md`: create or repair the single AppHost under
`local/aspire/`, the contract, the mode switch, evidence-backed resources with explicit references and waits; add every
new project to the solution and verify with `dotnet sln list`; consolidate an existing test AppHost with consent.

### Phase 4 — Database
`references/database.md`: one authoritative schema strategy (A–E) per database; the `{App}.DbInitializer` applies the
schema in every mode and the seed in Local mode; applications wait for it; no persistence.

### Phase 5 — Configuration and secrets
`references/apphost.md` § Configuration and secrets, `references/local-development.md` § Configuration and external
services: `WithReference` under the keys the code reads, secret parameters for external credentials, sanitized
examples only.

### Phase 6 — Local JWT authentication
`references/local-jwt-authentication.md`: choose the trust strategy per protected service, write the trust mapping in
`AppHostContract`, apply it in the AppHost, add `local/tools/devtoken.cs` + `devtoken.json` with profiles from the real
roles, scopes and policies, run `devtoken init`. Fix configurations that break rule 9 (under rule 6 for production
code) and verify the fix.

### Phase 7 — Observability
`references/observability.md`: reuse existing telemetry or add `src/{App}.ServiceDefaults`, dependency
instrumentation, no duplicate pipelines; test modes stay without dashboard or telemetry.

### Phase 8 — Test infrastructure
`references/architecture-and-layout.md` (projects, packages, xUnit flavour), `references/integration-testing.md`
(fixtures, base classes, serial execution), `references/database.md` (resetters, repositories, builders),
`references/local-jwt-authentication.md` (test users, `TestTokenIssuer`, guards), `references/messaging.md` and
`references/external-services.md` (brokers, WireMock, safety nets). Copy and adapt all templates of a project, then
build it (order in `references/architecture-and-layout.md` § Build checkpoints).

### Phase 9 — Tests
`references/integration-testing.md` (conventions, component and functional tests, authentication matrix) and
`references/unit-tests.md`. Setup adds a few smoke tests per project, including the authentication matrix of every
protected service; incremental work adds only what was asked.

### Phase 10 — Official Aspire AI agent integration
`references/local-development.md` § Official Aspire AI agent integration: `aspire agent init` from the repository root
with an explicit skill list; MCP only with consent.

### Phase 11 — Validation
`references/validation-and-checklist.md`: structure, build, tests (twice when new) with a fail proof, local start,
two-run check, smoke requests with devtoken profiles, telemetry, no-identity-provider evidence and guard sabotage,
production isolation, agent skills. Stop only the AppHosts you started.

### Phase 12 — Documentation
`local/local-development.md` from `assets/local/local-development.md` and `tests/README.md` from
`assets/tests/tests-README.md`, filled with verified values only (no tokens, guessed ports or unobserved results).
Keep existing docs and scripts consistent.

### Phase 13 — Final report
`references/validation-and-checklist.md` § Final report (setup, repair) or § Summary of incremental work. The work is
complete only when every applicable item of § Acceptance checklist is satisfied or reported as blocked with the reason.

## Reference map

| Read | When |
|---|---|
| `references/discovery.md` | Always first: what to inspect, signals, report templates, decisions to ask |
| `references/architecture-and-layout.md` | Creating or placing any project: layout, naming, references (incl. `Aliases="apphost"`), solution, TFMs, packages, xUnit flavour, csproj templates |
| `references/apphost.md` | Creating, repairing or consolidating the AppHost; modes; contract; resources; parameters |
| `references/database.md` | Schema strategy, initializer and seed, ephemeral rule and safety, schema and resets in tests, repositories, builders |
| `references/local-jwt-authentication.md` | Anything about tokens, trust, roles/claims/scopes, devtoken, `TestTokenIssuer`, auth tests, production isolation |
| `references/local-development.md` | Running and configuring Local mode, external services, the operational doc, `aspire agent init`, MCP, debugging |
| `references/observability.md` | ServiceDefaults, instrumentation, dashboard verification, secrets in telemetry, test-mode observability |
| `references/integration-testing.md` | Test types and modes of test work, writing tests, identity, fixtures, component and functional tests, xUnit v2 |
| `references/unit-tests.md` | Writing unit tests |
| `references/messaging.md` | Kafka, RabbitMQ, Service Bus, SQS: publishing, consuming and verifying messages |
| `references/external-services.md` | Substitute per dependency and mode, WireMock, MailPit, cloud emulators, "nothing leaves the machine" |
| `references/validation-and-checklist.md` | Verification commands, acceptance checklist, report table, final report |
| `references/sources.md` | Official documentation and version-specific verified behaviour |
| `assets/README.md` | Which template goes where, placeholders and what to adapt |
