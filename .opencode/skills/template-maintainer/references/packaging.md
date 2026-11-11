# Packaging the template

How the repository becomes a `dotnet new` template and a NuGet package, and the traps in each step.

## `dotnet new` from source

```bash
dotnet new install .                       # from a clone
dotnet new clean-webapi -n MyCompany.Orders -o MyCompany.Orders
dotnet new uninstall <path-to-this-repository>
```

`.template.config/template.json`:

| Field | Value |
|---|---|
| `sourceName` | `CleanWebApiTemplate` — replaced in file names, namespaces and content (lower-case forms too). |
| `shortName` | `clean-webapi`, `cleanwebapi` |
| `sources[0].modifiers[0].exclude` | The paths that never reach a generated project (see the skill). |
| `sources[0].modifiers[0].rename` | `README.template.md` → `README.md`. |
| `symbols.skipRestore` | `--no-restore`. |
| `symbols.ApiUserSecretsId`, `AppHostUserSecretsId` | GUID generators; `replaces` the two fixed GUIDs in the csproj files. |
| `postActions` | NuGet restore, `dotnet tool restore`, `chmod +x` on the hook scripts. |

Every occurrence of `CleanWebApiTemplate` in a shipped file is replaced. That is why the skill, the example
and the tests use literal names instead of `{App}` placeholders.

## The NuGet package

`build/CleanWebApiTemplate.Template.csproj`:

- `PackageType=Template`, `IncludeContentInPack`, `ContentTargetFolders=content`,
  `NoDefaultExcludes`, `EnableDefaultItems=false`, `IncludeBuildOutput=false`.
- `Content Include` for `src`, `tests`, `local`, `scripts` (minus the smoke scripts), `.config`, `.husky`,
  `.template.config`, `.vscode` and the root files (`AGENTS.md`, `README.template.md`, `global.json`,
  `Directory.*.props`, `.editorconfig`, …).
- `None Include` for `README.nuget.md` (as the package readme), `LICENSE` and `CHANGELOG.md`.

A new shipped folder must be added to the package project **and** its contents checked against the
`template.json` exclusions. The smoke test asserts both.

```bash
dotnet pack build/CleanWebApiTemplate.Template.csproj -c Release -o artifacts
dotnet nuget push artifacts/CleanWebApiTemplate.<version>.nupkg --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

## What the smoke tests assert

`scripts/template-smoke.sh`:

1. installs the source in a temporary `--debug:custom-hive`;
2. generates `Sample` (with restore) and `Sample2` (`--no-restore`);
3. checks the output: `README.md` and `.gitignore` present; excluded paths absent; no `CleanWebApiTemplate`
   left in names or content; distinct `UserSecretsId` per project and generation; executable hooks;
4. `git init` + `dotnet husky install` and checks `core.hooksPath`;
5. builds, runs the container-free tests and publishes AOT (unless `--no-aot`).

`scripts/package-smoke.sh`:

1. packs the template into a temporary `artifacts/`;
2. inspects the `.nupkg`: config, README, src/tests/local present; `build/`, audit, coverage, agent skills and
   smoke scripts absent; package readme/license/changelog at the root;
3. installs it in a temporary hive, generates `Sample`, checks the files;
4. builds, runs the container-free tests and publishes AOT;
5. uninstalls the package from the hive.

Options: `--keep` keeps the work directory; `--no-aot` skips the publish.

## Traps

| Trap | Symptom |
|---|---|
| A new folder is not in the package project | The generated project is missing files the source has. |
| A path is in the package but not excluded | `docs/audit`, skills or smoke scripts leak into generated projects. |
| A literal `CleanWebApiTemplate` in a file that should not be renamed | The generated project is renamed where it should not be. |
| A GUID is not regenerated | Two generated projects share user secrets. |
| A post-action script is not executable | `dotnet new` warns; hooks do not run. |
| The exclusions and the smoke assertions disagree | The smoke test fails on a path that is intentionally gone; update both. |
