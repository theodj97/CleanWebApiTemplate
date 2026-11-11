# Test prompts for `clean-webapi`

`evals.json` holds realistic prompts with expected results and verifiable expectations, in the skill-creator
schema. Run each prompt once with the skill installed and once without, starting from the same repository
state, then grade the run against the expectations.

## What each prompt needs

| # | Prompt | Target repository must have |
|---|---|---|
| 1 | Add a `Project` feature with create, get and filter | A generated repository (the `Todo` sample is fine); Docker for component/functional tests; Aspire for the local check |
| 2 | Add a component test for an existing endpoint | A generated repository with at least one protected endpoint and the component test fixture |
| 3 | Add a `Priority` column to `Todo` | A generated repository with the `Todo` sample and its schema/seed |
| 4 | Add a `Reports` role that can read every owner's projects | A generated repository with the `Project` feature from prompt 1 (or an equivalent owner-scoped feature) |
| 5 | Review a working tree with deliberate violations | A branch/worktree with: SQL built in an API handler, `Application` referencing `Infrastructure`, a new request missing from `AppJsonSerializerContext`, and a test using `Thread.Sleep` |
| 6 | Remove the sample `Todo` feature | A generated repository with the `Todo` sample, its tests, SQL and local examples |
| 7 | Add a secret for an external API key | A generated repository with OpenBao, the AppHost and `openbao-init` |

## Running them

1. Copy the target repository to a clean directory for each run.
2. Install `clean-webapi` in `.agents/skills/` (or `.claude/skills/`) for the "with skill" run; leave it out
   for the baseline.
3. Use the same agent/model, prompt and repository state for both runs.
4. Save the transcript and the diff.
5. Grade every expectation as pass/fail with evidence from the transcript or the diff.

Do not grade style preferences as hard failures. The hard expectations are the layer placement, the explicit
registrations, the AOT safety, the owner filter, the SQL safety, the test level and the gates. A run that
claims a gate passed without showing the command and exit code fails the honesty expectation.

## What to check after every run

- No secret, token or key in the diff, the transcript or the report.
- No `git add`/`commit`/`push` unless the prompt asked for it.
- No suppression (`RequiresDynamicCode`, `RequiresUnreferencedCode`, `UnconditionalSuppressMessage`) to make
  a warning disappear.
- No lowered coverage threshold.
- No test skipped without being reported.
- The final report names the commands that actually ran and the ones that could not run.
