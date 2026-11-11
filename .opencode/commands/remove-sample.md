---
description: Remove the Todo sample feature
agent: build
---

Remove the sample `Todo` feature and keep the solution building and the local environment starting.

1. Load the `clean-webapi` skill and read `references/feature-slice.md` § Removing the sample.
2. `git grep -n -i todo` and list every reference before editing.
3. Remove Api, Application, Domain, Infrastructure, SQL/seed, tests, test helpers, local Swagger entries,
   registrations and docs, in the order the reference gives. Fix the architecture-test anchors and the
   contract tests.
4. Run build, AOT publish, every test and `aspire start` (or explain why the local start could not run).
5. Report the real results and anything deliberately kept.
