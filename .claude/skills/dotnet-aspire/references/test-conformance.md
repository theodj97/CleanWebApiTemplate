# Test conformance: audit existing tests and refactor them to this skill's standards

*New (user decision).* Mode `conform-tests`. In every other mode existing tests keep their conventions
(`integration-testing.md` § Modes of test work); this mode is the explicit request to bring them to the standards of
this skill. It never changes what a test proves, never changes production code (SKILL.md rule 6), and works in
confirmed batches, each one verified by the full suite.

## Contents
- [Phase 1: baseline](#phase-1-baseline)
- [Phase 2: conformance audit](#phase-2-conformance-audit)
- [Findings catalogue](#findings-catalogue)
- [Phase 3: plan and confirmation](#phase-3-plan-and-confirmation)
- [Phase 4: refactor in batches](#phase-4-refactor-in-batches)
- [Phase 5: report](#phase-5-report)

## Phase 1: baseline

Before reading tests in depth, record what exists and how it behaves today:

```bash
dotnet test <solution> > baseline.log 2>&1; echo "exit=$?"        # per project: total, passed, failed, skipped
git grep -c -E '\[(Fact|Theory)' -- 'tests/*.cs' '*Tests*/*.cs'   # test methods per file (a theory counts once here)
```

- Keep the per-project counts and the names of tests that already fail: they are pre-existing, reported separately,
  never "fixed" by editing their assertions.
- No container runtime: the baseline is partial; say which projects could not run.
- Also run discovery § 3 and § 8 (`discovery.md`): test projects, framework and flavour, infrastructure (Testcontainers,
  docker-compose, a shared database, a test AppHost, `WebApplicationFactory`), how tests authenticate, conventions.

## Phase 2: conformance audit

Read every test project and match the catalogue below. Each finding has: file:line, the rule it breaks (with the
canonical reference), the proposed change, its category, and its risk.

Categories:

- **S — safe refactor**: local, mechanical, behaviour-preserving; the test proves the same thing afterwards.
- **I — infrastructure**: moves tests onto the skill's infrastructure (shared AppHost, fixtures, real engines, local
  JWT); changes how tests run, not what they assert.
- **R — rule violation**: breaks a non-negotiable rule (rules 7–9); fixing it is mandatory once the mode runs, and the
  fix may make hidden bugs visible.
- **M — migration**: framework or library changes; only when the user asks for that specific migration.

## Findings catalogue

| Id | Finding | Target (canonical reference) | Cat. |
|---|---|---|---|
| S1 | Fixed waits for eventual effects | `Eventually` / `WaitFor…` helpers (`integration-testing.md` § Asynchrony, time and determinism) | S |
| S2 | Async calls without the test's `CancellationToken` | pass `CancellationToken` (§ Serial execution and cancellation) | S |
| S3 | No Arrange/Act/Assert sections, several actions in Act, exceptions asserted with try/catch | AAA, one action, `Assert.ThrowsAsync(act)` (§ Writing tests) | S |
| S4 | Hard-coded ids (`/orders/1`) | ids from test repositories (`database.md` § ResetDatabase semantics) | S |
| S5 | Roles or identities as strings | `TestUser` values, `[AuthenticatedAs]`, `CreateClient(user)` (§ Identity) | S |
| S6 | Database reset after the test, or no reset where data is written | `[ResetDatabase]`, reset before (`database.md`) | S |
| S7 | Reading "the last message" of a topic | snapshot/probe before the Act + predicate (`messaging.md`) | S |
| S8 | Component/functional projects running in parallel | `parallelizeTestCollections: false` | S |
| S9 | Test names without a convention | `Method_Scenario_ExpectedResult` **only when the repository has no consistent convention** | S |
| I1 | Testcontainers, docker-compose or a shared developer database for integration tests | the shared AppHost in Component/Functional mode (`apphost.md`) | I |
| I2 | A dedicated test AppHost | consolidation (`apphost.md` § Consolidating a dedicated test AppHost) | I |
| I3 | Containers or apps started per class/test | one assembly fixture per project (§ The fixture) | I |
| I4 | Schema applied by tests while an initializer exists, or `EnsureCreated` next to migrations | the shared initializer (`database.md` § Schema in test modes) | I |
| I5 | Wrong test type: database/HTTP in unit projects, mocked `DbContext`/`DbSet` | move to component tests; keep the pure logic as unit tests (`unit-tests.md`) | I |
| I6 | Projects outside `tests/`, names not `{Deployable}.<Type>` | move/rename with consent (`architecture-and-layout.md`) | I |
| I7 | Missing authentication matrix on protected endpoints | add the matrix, additive (`local-jwt-authentication.md` § Authentication and authorization tests) | I |
| R1 | SQLite, EF Core InMemory or an in-memory broker as substitute | the production engine in a container (rule 8) | R |
| R2 | Tokens from a login endpoint or an identity provider | `TestTokenIssuer` + trust mapping (rule 9) | R |
| R3 | Accept-any-token or test authentication schemes; validation disabled | real JwtBearer validation with locally signed tokens (rule 9) | R |
| R4 | Real external URLs reachable from tests, no safety net | WireMock/emulators + safety nets (rule 8, `external-services.md`) | R |
| R5 | Secrets in test code or config | generated per run or local containers (rule 10) | R |
| M1 | xUnit v2 → v3, VSTest → MTP, NUnit/MSTest → xUnit | only on explicit request (`architecture-and-layout.md` § Choosing the xUnit packages) | M |
| M2 | Moq/FakeItEasy → NSubstitute, FluentAssertions/Shouldly → `Assert` | only on explicit request | M |

Search hints (candidates only; a finding exists after reading the code). Patterns are extended regexes for
`git grep -n -E '<pattern>' -- 'tests/*.cs'` from the repository root; exit code 1 means no match:

```text
S1  Thread\.Sleep|Task\.Delay\(          (the polling interval inside Eventually and comments are not findings)
S2  xUnit1051 warnings; Async\([^)]*\) without CancellationToken
S3  catch \( in tests; missing // Act
S4  "/[a-z-]+/[0-9]+
S5  "Admin"|Roles\s*= in tests
S6  Respawn|Reset in Dispose/After
S7  Consume\(|LastOrDefault in messaging helpers
S8  missing xunit.runner.json
S9  sample names
I1  Testcontainers|ContainerBuilder|docker-compose|Server=.*;Database=
I2  Aspire.AppHost.Sdk under tests/
I3  IClassFixture creating containers, new WebApplicationFactory per test
I4  EnsureCreated|MigrateAsync in tests
I5  Substitute\.For<.*(DbContext|DbSet)|Mock<.*(DbContext|DbSet)
I6  project paths
I7  protected routes without 401/403 tests
R1  UseSqlite|UseInMemoryDatabase|InMemoryTransport|UsingInMemory
R2  /auth/login|/connect/token|login in test helpers
R3  AuthenticationHandler<|TestAuthHandler|TestAuthenticationHandler|AddScheme<|ValidateIssuer\s*=\s*false|ValidateLifetime\s*=\s*false|RequireSignedTokens\s*=\s*false|SignatureValidator
R4  https?:// in test config; no BlockExternalHttpHandler/proxy
R5  password|secret|apikey|Bearer ey literals
M1  packages
M2  packages
```

Out of scope: production code smells found on the way (`DateTime.UtcNow` instead of `TimeProvider`, static caches) are
reported as improvements, not changed.

## Phase 3: plan and confirmation

Present the audit (counts per finding and category, the list with file:line), the baseline, and a plan of batches:

1. **R and I first**, because S changes rely on their helpers (fixtures, `TestUser`, `Eventually`, resetters): build or
   reuse the infrastructure with the Setup steps of SKILL.md Phase 8, then move tests onto it.
2. **S next**, one finding type per batch (all S1 together, then S2…).
3. **M** only when the user asked for it, last, one library per batch.

Wait for confirmation. The user may drop findings or whole categories; R findings that stay are reported as open rule
violations.

## Phase 4: refactor in batches

For each batch:

1. Apply only that batch's change. Keep each test's intent: same scenario, same expectations; a test that is split
   keeps every original assertion in one of its parts.
2. Build (no new warnings when warnings are errors) and run the **full** suite.
3. Accept the batch only if: no test that passed in the baseline fails now; the number of test cases per project did
   not drop (splits and new matrix tests may raise it); pre-existing failures are unchanged or explained.
4. For moved or rewritten tests, prove one per batch can fail (change its expected value, see it fail, revert).
5. Record the batch: findings fixed, files, test counts before/after.

Rules while refactoring:

- Never weaken, delete or skip an assertion, and never delete a test, to make a batch pass. A test that starts failing
  because the new infrastructure is more faithful (R1–R4: a real engine, real token validation) has found a bug:
  stop that batch, report it with the failing test, and ask; never adapt the assertion to the bug.
- Never change production code or authorization policies (rule 6); declared testability changes only.
- Additions follow the incremental rules (`integration-testing.md`): reuse fixtures, builders and repositories.
- Moving or renaming projects needs explicit consent (rule 6); deleting a superseded test infrastructure (a test
  AppHost, Testcontainers fixtures) happens only after the batch that replaced it passed.
- Keep a mapping table for renamed or moved tests (old name → new name), so CI history and reviewers can follow them.

## Phase 5: report

- Baseline and final counts per project; batches applied with their verification results.
- Conformance table: finding, before (count), after (count), open items with the reason (declined by the user, needs
  a production change, revealed bug).
- Renamed/moved tests mapping; bugs found (failing test + evidence); improvements for production code.
- The summary of `validation-and-checklist.md` § Summary of incremental work.
