---
name: template-maintainer
description: >-
  Maintains the CleanWebApiTemplate repository itself, not a generated project: dotnet new packaging
  (template.json, the NuGet template project, exclusions), the template and package smoke tests, the agent
  layer (AGENTS.md, .opencode skills/commands/agents), the CHANGELOG and the version, and the roadmap in
  docs/audit. Use when changing what the template ships, adding or removing template files, bumping the
  package version, running the smoke tests, editing the clean-webapi skill or its evals, or checking that a
  change reaches (or deliberately does not reach) a generated project. Not for feature work in a generated
  project: use clean-webapi for that.
---

# Maintaining the template

This repository is two things at once:

- the **source** of the `dotnet new` template (`shortName: clean-webapi`), and
- a **working solution** that must build, test and publish as is.

A change usually has to keep both true. This skill covers the parts that only exist in the template
repository and are **not** shipped to generated projects.

## What the generated project receives

`dotnet new` copies the repository except the paths excluded in `.template.config/template.json`:

```text
.git, .husky/_ , .agents, .task-skill, .tools, .vs, .idea,
skills-lock.json, coverage-report, docs/audit, node_modules,
scripts/template-smoke.sh, scripts/package-smoke.sh,
build, artifacts, *.nupkg, *.snupkg,
CHANGELOG.md, LICENSE, README.md, README.nuget.md,
**/TestResults, **/bin, **/obj
```

and renames `README.template.md` to `README.md`. The NuGet package is assembled separately by
`build/CleanWebApiTemplate.Template.csproj`, which lists the folders it packs; a new shipped folder must be
added there **and** kept out of the exclusions.

Shipped on purpose (do not exclude):

- `AGENTS.md`, `GLOSSARY.md`, `docs/adr/`;
- `.opencode/skills/clean-webapi`, `.opencode/commands`, `.opencode/agents`;
- `tests/README.md`, `local/`, `scripts/` (except the two smoke scripts);
- `.editorconfig`, `.gitattributes`, `.gitleaks.toml`, hooks, `Directory.*.props`, `global.json`.

Excluded on purpose:

- `.opencode/skills/template-maintainer` (this skill);
- `docs/audit/` (the template's audit trail);
- `build/`, `artifacts/`, the package smoke scripts, the repository README and the changelog.

## The agent layer

| Path | Ships | Purpose |
|---|---|---|
| `AGENTS.md` | yes | The non-negotiables and the pointer to the skill. |
| `GLOSSARY.md` | yes | Domain language only. |
| `docs/adr/` | yes | Decisions that are hard to reverse. |
| `.opencode/skills/clean-webapi/` | yes | The detailed rules, references and the `Project` example. |
| `.opencode/skills/template-maintainer/` | no | This skill. |
| `.opencode/commands/` | yes | `/feature`, `/endpoint`, `/verify`, `/remove-sample`. |
| `.opencode/agents/reviewer.md` | yes | Read-only review subagent. |

The **skill is the single source of truth** for the agent layer. It uses literal `CleanWebApiTemplate` names so
`dotnet new` rewrites them like the rest of the code; never introduce `{App}` placeholders. If the code
changes, the skill changes in the same commit.

## Workflow

1. Make the code change (use the `clean-webapi` skill for the code rules).
2. Update the skill: `SKILL.md` and the reference the change touches; update the `Project` example when a
   pattern changes (the example mirrors the code, it is not a separate design).
3. Update `AGENTS.md` only if a non-negotiable changed; the detail belongs in the skill.
4. Run the gates: build, AOT publish, all tests, `npm run verify`, `npm run coverage`.
5. Run the template smoke test and, if the packaging changed, the package smoke test.
6. Update `CHANGELOG.md` under `[Unreleased]` and bump `Version` in `build/…Template.csproj` when releasing.
7. Do not create or modify CI workflows: the repository forbids it. Leave the commands ready and tell the user.

## Smoke tests

```bash
bash scripts/template-smoke.sh          # install from source in an isolated hive, generate two projects, build/test/AOT
bash scripts/template-smoke.sh --no-aot --keep
bash scripts/package-smoke.sh           # pack the .nupkg, install it, generate and validate
bash scripts/package-smoke.sh --no-aot --keep
```

The template smoke test checks the generated files (README, `.gitignore`, no excluded paths, distinct
`UserSecretsId`, executable hooks), installs Husky, builds, runs the container-free tests and publishes AOT.
The package smoke test inspects the `.nupkg` content, installs it in a temporary hive and does the same.

When you add a shipped folder or file, update both scripts and the package project; when you exclude one,
update them too. A smoke test that still asserts a path you deleted is a broken gate, not a warning.

## The clean-webapi skill

```
.opencode/skills/clean-webapi/
├── SKILL.md                  entry point: non-negotiables, modes, phases, reference map
├── references/*.md           one per topic (architecture, API, persistence, …)
├── assets/examples/          the Project feature, mirroring src/ and tests/
└── (no evals in the shipped skill)
```

Rules for editing it:

- Every statement must match the code. Cite the file in the reference when a rule is easy to verify.
- Keep `SKILL.md` short: the detail goes in a reference.
- The example is code: it must compile when copied. Do not describe a pattern in the example that the
  references contradict.
- No companion-skill dependencies: the shipped skill stands alone.
- `template-maintainer/evals/` holds the prompts used to check the skill; run them manually after a
  substantial rewrite and record the result in the verification log.

## Files only this repository has

| Path | Why it is not shipped |
|---|---|
| `.template.config/template.json` | The template definition. |
| `build/CleanWebApiTemplate.Template.csproj` | The NuGet package project. |
| `scripts/template-smoke.sh`, `scripts/package-smoke.sh` | Validate the template, not a generated project. |
| `README.md`, `README.nuget.md`, `CHANGELOG.md`, `LICENSE` | Repository/package docs. |
| `docs/audit/` | The audit roadmap and evidence. |
| `.agents/`, `skills-lock.json`, `.tools/`, `coverage-report/`, `artifacts/` | Local tooling and outputs. |
| `.opencode/skills/template-maintainer/` | This skill. |

`docs/audit/` is in Spanish and is not distributed. The rest of the documentation is in English.

## Versioning

- `CHANGELOG.md` follows Keep a Changelog; add under `[Unreleased]`.
- The version lives in `build/CleanWebApiTemplate.Template.csproj` (`<Version>`), not in the solutions.
- A change to what the template generates is at least a minor bump; a rename of a shipped path is a major one.
- The package ID is `CleanWebApiTemplate`; the short name `clean-webapi` is stable.

## Checklist

- [ ] The code builds and publishes AOT with zero warnings.
- [ ] All tests pass; coverage thresholds met.
- [ ] `AGENTS.md`, `GLOSSARY.md` and the skill match the code.
- [ ] The `Project` example still mirrors the code.
- [ ] The shipped/excluded lists are intentional and both smoke tests agree.
- [ ] `CHANGELOG.md` updated; version bumped if releasing.
- [ ] No secrets, no local paths, no machine-specific values in the shipped files.
