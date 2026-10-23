# Architecture, layout, naming, solution and project files

Canonical for SKILL.md rules 1–3: where every project lives, how it is named, how it joins the solution and how its
project file and packages are chosen. The AppHost itself is in `apphost.md`.

## Contents
- [Target layout](#target-layout)
- [Folder responsibilities](#folder-responsibilities)
- [Project naming](#project-naming)
- [Project references](#project-references)
- [Adding projects to the solution](#adding-projects-to-the-solution)
- [Target frameworks](#target-frameworks)
- [Packages and versions](#packages-and-versions)
- [Choosing the xUnit packages](#choosing-the-xunit-packages)
- [Project files](#project-files)
- [Build checkpoints](#build-checkpoints)

## Target layout

```text
<repo-root>/
├── <Solution>.sln | <Solution>.slnx           existing, reused (never a second one)
├── src/                                       application + shared runtime code, deployed to every environment
│   ├── <existing projects>
│   └── {App}.ServiceDefaults/                 only if justified (observability.md)
├── tests/                                     every automated test, nothing that orchestrates
│   ├── {Deployable}.UnitTests/                one trio per deployable application (e.g. Contoso.Orders.Api.UnitTests)
│   ├── {Deployable}.ComponentTests/
│   ├── {Deployable}.FunctionalTests/
│   ├── {App}.Tests.Shared/                    one per repository: fixtures, auth, data, helpers
│   ├── Directory.Build.props                  optional: test-only properties (IsPackable=false)
│   └── README.md
└── local/                                     everything local-only, nothing in src/ depends on it
    ├── aspire/
    │   ├── {App}.AppHost/                     THE AppHost: local development and tests (apphost.md)
    │   └── {App}.DbInitializer/               schema (+ seed in Local mode) on every start, when there is a database
    ├── database/
    │   ├── seed/                              NNN_*.sql synthetic seed, Local mode only
    │   └── schema/                            only local-only/inferred schema scripts (canonical ones stay where they are)
    ├── tools/
    │   ├── devtoken.cs                        local JWT tool (local-jwt-authentication.md)
    │   └── devtoken.json                      issuer, audiences, claim names, profiles (no secrets)
    ├── scripts/                               local-only helper scripts, only if they hold something
    └── local-development.md
```

This is a repository convention, not an Aspire requirement, and a target, not a reason to move working code: adapt it
to the repository's real conventions (an existing `local/{App}.AppHost/` stays where it is in repair mode; moving it
needs consent, `apphost.md` § Repairing). Never restructure existing application code to fit it.

## Folder responsibilities

| Folder | Holds | Never holds |
|---|---|---|
| `src/` | Code of the applications and code they run with in every environment (ServiceDefaults is shared runtime code). | Code whose only purpose is local orchestration, developer tooling or tests; Aspire hosting packages; anything referencing `local/` or `tests/`. |
| `tests/` | Unit, component and functional test projects, their shared library and README. | An AppHost, orchestration scripts, local tools, business code. *Changed:* the former `tests/orchestration/` folder is gone; its AppHost merged into `local/aspire/{App}.AppHost/` and its scripts into `local/database/`. |
| `local/` | The AppHost, local-only workers (DbInitializer), seed and local schema scripts, local tools, scripts, `local-development.md`. | Business code, tests, secrets, generated keys. |

Folder hygiene:

- Do not create empty folders: `local/database/`, `local/tools/` and `local/scripts/` exist only when they hold
  something.
- `local/` must not be git-ignored: `aspire ls` skips git-ignored directories.
- Local keys and secrets never live in the repository, so no `.gitignore` entry is needed for them
  (`local-jwt-authentication.md` § Key storage). When the repository already keeps populated local files (for example
  `appsettings.Local.json`), add them to the existing `.gitignore` without rewriting its rules.

## Project naming

`{App}` is the application name, determined in this order:

1. the common prefix of the existing application projects (`Contoso.Orders` from `Contoso.Orders.Api`,
   `Contoso.Orders.Domain`), keeping its exact casing and dots;
2. otherwise the solution file name (`Orders.slnx` gives `Orders`);
3. if neither is clear (several candidate prefixes, a generic solution name), ask the user and report the choice in
   the plan.

Every project this skill creates is named after the application and its folder equals the project name:

| Role | Folder / project |
|---|---|
| AppHost (local + tests) | `local/aspire/{App}.AppHost/{App}.AppHost.csproj` |
| Database initializer (schema, + seed in Local mode) | `local/aspire/{App}.DbInitializer/{App}.DbInitializer.csproj` |
| Shared service defaults | `src/{App}.ServiceDefaults/{App}.ServiceDefaults.csproj` |
| Local JWT tool | `local/tools/devtoken.cs` (file-based app, no project); `local/tools/{App}.DevToken/` when the SDK is older than .NET 10 |
| Unit / component / functional tests | `tests/{Deployable}.UnitTests`, `.ComponentTests`, `.FunctionalTests` (`{Deployable}` = the tested project, e.g. `Contoso.Orders.Api.ComponentTests`) |
| Shared test library | `tests/{App}.Tests.Shared/` (one per repository, named after the application/product) |

Test projects follow `{App}.<Role>` too: `{Deployable}` is itself an `{App}.*` project, so
`Contoso.Orders.Api.ComponentTests` is `{App}` + `Api.ComponentTests`. The csproj templates below use `{App}.Api` as
the example deployable.

- Never use generic names (`AppHost`, `MyApp.AppHost`, `Repository.AppHost`, `local/AppHost/`) or the repository
  folder name when it differs from the application name. A `dotnet new`, `aspire init` or `aspire new` template with
  another name is created with `-n {App}.<Role>` or renamed before it is added to the solution.
- Follow an existing naming convention for test projects if the repository has one.
- In repair mode keep existing project names: renaming an existing AppHost needs the user's consent, like moving it.
- The generated `Projects.*` type replaces dots and dashes with underscores: `Contoso.Orders.Api` becomes
  `Projects.Contoso_Orders_Api`.
- Root namespace = project name, unless `Directory.Build.props` sets another convention.
- *Superseded:* the optional `tests/{App}.AspireTests` project of the former local-environment skill is covered by
  the functional tests, which already exercise the AppHost model.

## Project references

| Project | References |
|---|---|
| `src/*` | Each other and `src/{App}.ServiceDefaults` only. Never `local/` or `tests/`. Check: `grep -rn --include='*.csproj' -E '(local\|tests)[/\\]' src` prints nothing. |
| `{App}.AppHost` | Executable projects it runs (the SDK marks them non-runtime references, so nothing is copied into the AppHost) and `{App}.DbInitializer`. A plain library that is not a resource needs `IsAspireProjectResource="false"`. |
| `{App}.DbInitializer` | The project that owns the `DbContext`/migrations or schema scripts, and ServiceDefaults. |
| `*.UnitTests` | The tested project (and libraries whose classes are tested). Never Shared, never the AppHost: unit tests bring no Aspire. |
| `{App}.Tests.Shared` | Projects that own the `DbContext`/entities/contracts used by test repositories and builders. Never the AppHost (avoids a second `Projects.*` type and AppHost dependencies everywhere). Links `AppHostContract.cs` as source. |
| `*.ComponentTests` | Shared, the AppHost **with `Aliases="apphost"`**, and the tested application (for `WebApplicationFactory<Program>`). |
| `*.FunctionalTests` | Shared and the AppHost **with `Aliases="apphost"`**. Referencing the app for DTOs is allowed, using its DI is not. |

Why the alias (*new*, found in a real repository and verified): on .NET 10 both the AppHost and every API with
top-level statements expose a public `Program` class, so a test project that references both fails with `CS0433`.
Reference the AppHost as `<ProjectReference Include="...{App}.AppHost.csproj" Aliases="apphost" />` and write
`extern alias apphost;` + `typeof(apphost::Projects.{App}_AppHost)` in the fixtures.

Why `tests/` may reference `local/`: only deployable code is forbidden to depend on `local/` (SKILL.md rule 2). Test
projects are never deployed, and the single AppHost (rule 4) is what they orchestrate.

Shared libraries consumed by new local projects must resolve the same package versions as the existing consumers.
When a library gets EF Core through a `PrivateAssets="all"` package such as `Microsoft.EntityFrameworkCore.Design`, a
new consumer can resolve an older transitive EF Core and fail with `CS1705` (reproduced again on 2026-10-23 with
Npgsql 10.0.3). Fix it with CPM or explicit matching `PackageReference`s in the library, never by upgrading the
application.

## Adding projects to the solution

```bash
dotnet sln <Solution>.slnx add --solution-folder local local/aspire/{App}.AppHost/{App}.AppHost.csproj
dotnet sln <Solution>.slnx add --solution-folder src src/{App}.ServiceDefaults/{App}.ServiceDefaults.csproj
dotnet sln <Solution>.slnx add --solution-folder tests tests/{App}.Tests.Shared tests/{App}.Api.UnitTests ...
dotnet sln <Solution>.slnx list
```

- Discover the authoritative solution first (`discovery.md` § 1). Never create a second solution, never convert between
  `.sln` and `.slnx`.
- Works for `.sln` and, from SDK 9.0.200, `.slnx`. On older SDKs edit the `.slnx` XML by hand (folder names start and
  end with `/`).
- Mirror existing folder names and casing. New local projects go in one `local` folder, test projects in one `tests`
  folder, unless the solution already nests folders by path (then mirror it, e.g. `--solution-folder local/aspire`).
  `--solution-folder src` adds to the existing `/src/` folder (verified with `.slnx`). `--solution-folder` and
  `--in-root` are mutually exclusive.
- `dotnet sln` only manages projects. Add solution items (`local/local-development.md`, scripts) by hand only if the
  repository already uses solution items. `devtoken.cs` is not a project and is never added.
- Verify: every new csproj appears in `dotnet sln … list`, and `dotnet build <solution>` builds it.

## Target frameworks

- Match the repository: the highest application TFM the installed SDK supports for the AppHost and the initializer;
  test projects target the TFM of the app they test. A project that references both the app and the AppHost targets a
  TFM greater than or equal to both. The AppHost template supports `net8.0`–`net11.0` in 13.6.x.
- Aspire requires net8.0 or later for the AppHost and test projects. If an app targets an older framework, ask:
  component tests cannot host it in-process from a newer test project without risk.
- Multi-targeted apps: test the highest TFM unless the user asks for more.
- `devtoken.cs` needs the .NET 10 SDK (file-based apps). With an older pinned SDK use the console-project variant
  (`local-jwt-authentication.md` § Local development: devtoken).

## Packages and versions

- Always respect the libraries and versions the repository already uses. A package already used: same version.
- A new package: the latest **stable** version compatible with the TFM, resolved at run time
  (`dotnet package search <id> --exact-match --format json`, or the NuGet flat-container index, skipping prereleases);
  use the repository's private feeds when `NuGet.config` defines them.
- Families line up: `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.Extensions.*` with the
  app's major; all `Aspire.*` packages and the `Aspire.AppHost.Sdk` share one Aspire version (the repository's if it
  already uses Aspire); `WireMock.Net.Aspire` is the latest version whose `Aspire.Hosting` dependency is not newer than
  the Aspire version in use.
- Preview packages need explicit consent and a note in the documentation.
- **CPM** (`Directory.Packages.props` with `ManagePackageVersionsCentrally=true`): versions go there, following its
  style (order, groups, comments, conditions per TFM), and `PackageReference` has no `Version`. `dotnet add package` and
  `aspire add` append or write versions: move them to their group. Note `CentralPackageTransitivePinningEnabled`.
- **`Directory.Build.props`** applies to new projects (including `local/` and `tests/`): generated code must build
  under its settings (`TreatWarningsAsErrors`, analyzers, `Nullable`). Test-only properties go in the test csproj files
  or a `tests/Directory.Build.props` that imports the parent:
  ```xml
  <Project>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
    <PropertyGroup><IsPackable>false</IsPackable></PropertyGroup>
  </Project>
  ```

Defaults per project when the repository has nothing comparable:

| Project | Packages |
|---|---|
| `{App}.AppHost` | the SDK brings `Aspire.Hosting.AppHost`; one hosting integration per resource (`Aspire.Hosting.PostgreSQL`, `.SqlServer`, `.Redis`, `.Kafka`, `WireMock.Net.Aspire` for test-mode third parties…) |
| `{App}.DbInitializer` | the provider the app already uses (through the referenced data project) |
| `*.UnitTests` | xUnit set (below), `NSubstitute`, `NSubstitute.Analyzers.CSharp`, `Bogus` (if useful), `Microsoft.Extensions.TimeProvider.Testing` (if the app uses `TimeProvider`) |
| `{App}.Tests.Shared` | `xunit.v3.extensibility.core`, `xunit.v3.assert`, `Aspire.Hosting.Testing`, `Respawn`, the DB driver(s), broker clients as needed, `WireMock.Net.Aspire` (brings `WireMock.Net.RestClient`) when there are third parties, `Microsoft.IdentityModel.JsonWebTokens`, `Bogus` |
| `*.ComponentTests` | xUnit set, `Aspire.Hosting.Testing`, `Microsoft.AspNetCore.Mvc.Testing` (app's ASP.NET Core major), `Microsoft.Extensions.TimeProvider.Testing` if needed |
| `*.FunctionalTests` | xUnit set, `Aspire.Hosting.Testing` |

Never add to test projects: ServiceDefaults, OpenTelemetry packages, Testcontainers (Aspire orchestrates the
containers), SQLite or EF Core InMemory providers, extra assertion libraries (xUnit's `Assert` unless the repository
uses another). Defaults when the repository has none: NSubstitute (mocks), xUnit `Assert`, Bogus (data), Respawn
(reset), WireMock.Net (third-party APIs).

## Choosing the xUnit packages

1. **Tests already exist**: keep their xUnit version and package flavour for new projects. Never migrate (v2 → v3,
   3.x → 4.x, VSTest → MTP) unless asked.
2. **No tests yet**: xUnit v3. The flavour depends on how `dotnet test` runs in the repository:
   - `global.json` contains `"test": { "runner": "Microsoft.Testing.Platform" }` → `xunit.v3` (Microsoft Testing
     Platform v2 in xunit.v3 4.x);
   - otherwise (VSTest, the default) → `xunit.v3.mtp-off` + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk`.
     On the .NET 10+ SDK plain `xunit.v3` 4.x **fails** in VSTest mode, and switching `global.json` to MTP would force
     every test project onto MTP: propose it in the report instead of switching silently.
   - Re-check these names on nuget.org when the installed xunit.v3 major differs from 4.x.
3. xUnit v3 test projects are executables (`<OutputType>Exe</OutputType>`). The Shared library is not a test project:
   it references `xunit.v3.extensibility.core` (+ `xunit.v3.assert`).

## Project files

Templates assume CPM and xUnit v3 in VSTest mode; drop properties that `Directory.Build.props` already defines. They
are written here, not shipped as asset files, because stray `.csproj` files inside a skill folder can be picked up by
repository-wide build scripts.

**`local/aspire/{App}.AppHost/{App}.AppHost.csproj`** (hand-written method, `apphost.md` § Creating the AppHost)
```xml
<Project Sdk="Aspire.AppHost.Sdk/<aspire version>">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework><tfm></TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AspireUseCliBundle>true</AspireUseCliBundle>
    <UserSecretsId><new GUID></UserSecretsId>
    <!-- AppHostContract.cs is linked into tests/{App}.Tests.Shared: internal here, public there. -->
    <DefineConstants>$(DefineConstants);APPHOST</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.PostgreSQL" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\src\{App}.Api\{App}.Api.csproj" />
    <ProjectReference Include="..\{App}.DbInitializer\{App}.DbInitializer.csproj" />
  </ItemGroup>
</Project>
```
Aspire 9.x uses the older shape (`Microsoft.NET.Sdk` + `<Sdk Name="Aspire.AppHost.Sdk" Version="9.x" />` +
`IsAspireHost` + a `PackageReference` to `Aspire.Hosting.AppHost`): match the repository's existing AppHost.

**`local/aspire/{App}.DbInitializer/{App}.DbInitializer.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <ItemGroup>
    <ProjectReference Include="..\..\..\src\{App}.Data\{App}.Data.csproj" />
    <ProjectReference Include="..\..\..\src\{App}.ServiceDefaults\{App}.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

**`tests/{App}.Api.UnitTests/{App}.Api.UnitTests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-off" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\{App}.Api\{App}.Api.csproj" />
  </ItemGroup>
</Project>
```

**`tests/{App}.Tests.Shared/{App}.Tests.Shared.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.extensibility.core" />
    <PackageReference Include="xunit.v3.assert" />
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="Respawn" />
    <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" />
    <PackageReference Include="Bogus" />
  </ItemGroup>
  <ItemGroup>
    <!-- Owned by the AppHost (local/); public here, internal there. -->
    <Compile Include="..\..\local\aspire\{App}.AppHost\AppHostContract.cs" Link="AppHostContract.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\{App}.Data\{App}.Data.csproj" />
  </ItemGroup>
</Project>
```

**`tests/{App}.Api.ComponentTests/{App}.Api.ComponentTests.csproj`** (FunctionalTests: the same without
`Microsoft.AspNetCore.Mvc.Testing` and without the application reference)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-off" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\{App}.Tests.Shared\{App}.Tests.Shared.csproj" />
    <!-- Aliased: the AppHost and the API both expose a public Program class (CS0433 otherwise). -->
    <ProjectReference Include="..\..\local\aspire\{App}.AppHost\{App}.AppHost.csproj" Aliases="apphost" />
    <ProjectReference Include="..\..\src\{App}.Api\{App}.Api.csproj" />
  </ItemGroup>
</Project>
```

`xunit.runner.json` (`assets/tests/xunit.runner.json`, `"parallelizeTestCollections": false`) goes in every
component and functional project, copied to the output directory. Use the JSON file, not
`[assembly: CollectionBehavior(DisableTestParallelization = true)]`, which is a compile error in xunit.v3 4.x.

## Build checkpoints

Build project by project so errors stay small: AppHost (with DbInitializer) → UnitTests → Shared → ComponentTests →
FunctionalTests → whole solution. A project builds only once all of its templates are in place: Shared needs the
attributes, auth, database, data, helpers and fixtures together. Run unit tests as soon as they exist; run component
and functional tests once their fixture compiles.
