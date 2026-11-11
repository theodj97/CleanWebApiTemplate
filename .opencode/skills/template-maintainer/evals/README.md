# Evals

Manual prompts that check the `clean-webapi` skill against this repository. They are not shipped and they are
not run automatically.

## How to run one

1. Generate a healthy copy of the template (`dotnet new clean-webapi -n Sample -o /tmp/sample`) or work on a
   branch with a clean tree.
2. Give the prompt to a fresh agent session with the skill available.
3. Compare the result with the expectations; record the gaps in this repository's skill, not in the generated
   copy.
4. Fix the skill (and the `Project` example) and re-run the prompt.

## What each prompt needs

| Eval | Repository state |
|---|---|
| 1 | Freshly generated project with the Todo sample present. |
| 2 | The Todo sample present. |
| 3 | The Todo sample present; Docker for the component test. |
| 4 | The Todo sample present; a project feature to protect (use the example) or `Todo`. |
| 5 | A deliberately broken working tree (layer violation, SQL in the Api, missing JSON entry, a `Thread.Sleep`). |
| 6 | A fresh generated project; Docker for a local start. |

`evals.json` holds the prompts, the expected output and the verifiable expectations. The numbering and the
expectations are the contract: an eval that passes by weakening an expectation is worse than a failing one.
