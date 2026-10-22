# Project setup: projects, packages, solution, test AppHost

Read this in Setup mode (or when a missing test project must be added). It covers creating the projects, choosing
packages and versions, wiring the solution and writing the test-only Aspire AppHost.

## Contents
- [Layout and naming](#layout-and-naming)
- [Target frameworks](#target-frameworks)
- [Choosing the xUnit packages](#choosing-the-xunit-packages)
- [Packages per project](#packages-per-project)
- [Resolving versions](#resolving-versions)
- [Central package management and Directory.Build.props](#central-package-management-and-directorybuildprops)
- [Project files](#project-files)
- [Adding the projects to the solution](#adding-the-projects-to-the-solution)
- [The test AppHost](#the-test-apphost)
- [tests/README.md](#testsreadmemd)
- [Build checkpoints](#build-checkpoints)

## Layout and naming

```
tests/
├── orchestration/
│   ├── {App}.Tests.AppHost/   # test-only Aspire AppHost
│   └── database/              # schema or seed scripts, only when needed
├── {App}.UnitTests/
├── {App}.ComponentTests/
├── {App}.FunctionalTests/
├── {App}.Tests.Shared/
└── README.md
```

- `{App}` is the deployable project's name (e.g. `Orders.Api` → `Orders.Api.UnitTests`). Follow an existing naming
  convention for test projects if the repo has one.
- Several deployable apps: one Unit/Component/Functional trio per app, but a single AppHost and a single Shared
  project named after the product or solution (`{Product}.Tests.AppHost`, `{Product}.Tests.Shared`).
- Root namespace = project name, unless `Directory.Build.props` sets another convention.

Project references:

| Project | References |
|---|---|
| UnitTests | the app project (and the libraries whose classes are tested) — never Shared, which brings Aspire |
| Tests.Shared | the app projects that own the DbContext/entities/contracts used by test repositories and builders. Not the AppHost. |
| ComponentTests | Shared, the AppHost (for the generated `Projects.*` type), the app project (for `WebApplicationFactory<Program>`) |
| FunctionalTests | Shared, the AppHost (the app runs as an Aspire resource; referencing its project for DTOs is fine, using its DI is not) |
| Tests.AppHost | every app it starts in functional mode; `TestHostContract.cs` from Shared linked as a source file |

`TestHostContract.cs` lives in Shared and is linked into the AppHost (see the AppHost csproj below): both need the same
names, and linking avoids a Shared → AppHost reference, which could generate a second `Projects.*` type and drag
AppHost dependencies into every test project.

## Target frameworks

- Test projects target the same TFM as the app they test. A project that references both the app and the AppHost
  must target a TFM greater than or equal to both.
- Aspire requires net8.0 or later for the AppHost and the test projects. If the app targets an older framework, ask:
  component tests cannot host it in-process from a newer test project without risk.
- Multi-targeted apps: test the highest TFM unless the user asks for more.

## Choosing the xUnit packages

1. **Tests already exist**: keep their xUnit version and package flavour for new projects. Never migrate (v2 → v3,
   3.x → 4.x, VSTest → MTP) unless asked.
2. **No tests yet**: xUnit v3. Pick the flavour from how `dotnet test` runs in this repository:
   - `global.json` contains `"test": { "runner": "Microsoft.Testing.Platform" }` → `xunit.v3` (Microsoft Testing
     Platform v2 in xunit.v3 4.x).
   - otherwise (VSTest mode, the default) → `xunit.v3.mtp-off` + `xunit.runner.visualstudio` +
     `Microsoft.NET.Test.Sdk`. On the .NET 10+ SDK, plain `xunit.v3` 4.x **fails** in VSTest mode, and switching
     `global.json` to MTP would force every test project of the repository onto MTP — so do not switch it silently;
     propose it in the report instead.
   - Re-check these package names on nuget.org when the installed xunit.v3 major version differs from 4.x: the
     flavours changed between 3.x and 4.x.
3. xUnit v3 test projects must be executables (`<OutputType>Exe</OutputType>`); the Shared library is not a test
   project and references `xunit.v3.extensibility.core` (+ `xunit.v3.assert` if it asserts) instead.

## Packages per project

Respect libraries the repository already uses (mocking, assertions, data). Defaults when there are none:

| Project | Packages |
|---|---|
| UnitTests | xUnit set (above), `NSubstitute`, `NSubstitute.Analyzers.CSharp`, `Bogus` (if useful), `Microsoft.Extensions.TimeProvider.Testing` (if the app uses `TimeProvider`) |
| Tests.Shared | `xunit.v3.extensibility.core`, `xunit.v3.assert`, `Aspire.Hosting.Testing`, `Respawn`, the DB driver(s) (`Npgsql`, `Microsoft.Data.SqlClient`, `MySqlConnector`, `Oracle.ManagedDataAccess.Core`, `MongoDB.Driver`, `StackExchange.Redis`), `Confluent.Kafka` / `RabbitMQ.Client` / `Azure.Messaging.ServiceBus` as needed, `WireMock.Net.Aspire` (brings `WireMock.Net.RestClient`), `Microsoft.IdentityModel.JsonWebTokens`, `Bogus` |
| ComponentTests | xUnit set, `Aspire.Hosting.Testing`, `Microsoft.AspNetCore.Mvc.Testing` (same major as the app's ASP.NET Core), `Microsoft.Extensions.TimeProvider.Testing` (if needed) |
| FunctionalTests | xUnit set, `Aspire.Hosting.Testing` |
| Tests.AppHost | Aspire hosting packages for each resource (`Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Kafka`, `WireMock.Net.Aspire`, … see external-services.md) |

Do not add: ServiceDefaults, OpenTelemetry packages, Testcontainers (Aspire orchestrates the containers), SQLite or
EF InMemory providers, assertion libraries.

## Resolving versions

- A package already used in the repository: same version.
- A new package: the latest **stable** version compatible with the TFM.
  ```bash
  dotnet package search Respawn --exact-match --format json      # all versions of one package
  ```
  or `GET https://api.nuget.org/v3-flatcontainer/{lowercase-id}/index.json` (includes prereleases: skip them).
  Use the repository's private feeds when `NuGet.config` defines them.
- Version families must line up:
  - `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.Extensions.*` → same major as
    the app (an app on .NET 8 uses 8.x).
  - Aspire: the version the repository already uses (development AppHost, client integrations); otherwise the latest
    stable that supports the installed SDK and TFM. All `Aspire.*` packages and the AppHost SDK share that version.
  - `WireMock.Net.Aspire`: the latest version whose `Aspire.Hosting` dependency is not newer than the Aspire version in
    use (check the package's dependencies on nuget.org).

## Central package management and Directory.Build.props

- With `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`): add `<PackageVersion>` entries there,
  following its style (alphabetical order, item groups by category, comments, conditions per TFM), and keep the
  `<PackageReference>` items in the csproj without versions. `dotnet add package` does this automatically (SDK 7.0.200+)
  but appends at the end: move the entry to its proper group afterwards.
- `Directory.Build.props` applies to the new projects too. If it sets `TreatWarningsAsErrors`, generated code must be
  warning-free (xUnit analyzers included). Put test-only properties (`IsPackable=false`) in the test csproj files or
  in a `tests/Directory.Build.props` that imports the parent one:
  ```xml
  <Project>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
    <PropertyGroup><IsPackable>false</IsPackable></PropertyGroup>
  </Project>
  ```
- Without CPM: versions in each csproj.

## Project files

Templates below assume CPM (no versions) and xUnit v3 in VSTest mode; adjust the xUnit set as decided above and drop
properties that `Directory.Build.props` already defines. These are not shipped as asset files on purpose: stray
`.csproj` files inside a skill folder can be picked up by repository-wide build scripts.

**{App}.UnitTests.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-off" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" PrivateAssets="all" />
    <!-- Only when the app uses TimeProvider: -->
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Orders.Api\Orders.Api.csproj" />
  </ItemGroup>
</Project>
```

**{App}.Tests.Shared.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3.extensibility.core" />
    <PackageReference Include="xunit.v3.assert" />
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="WireMock.Net.Aspire" />
    <PackageReference Include="Respawn" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Confluent.Kafka" />
    <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" />
    <PackageReference Include="Bogus" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Orders.Api\Orders.Api.csproj" />
  </ItemGroup>
</Project>
```

**{App}.ComponentTests.csproj** (FunctionalTests is identical without `Microsoft.AspNetCore.Mvc.Testing` and the app reference)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
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
    <ProjectReference Include="..\Orders.Api.Tests.Shared\Orders.Api.Tests.Shared.csproj" />
    <ProjectReference Include="..\orchestration\Orders.Api.Tests.AppHost\Orders.Api.Tests.AppHost.csproj" />
    <ProjectReference Include="..\..\src\Orders.Api\Orders.Api.csproj" />
  </ItemGroup>
</Project>
```

`xunit.runner.json` (copy `assets/xunit.runner.json`) disables parallel execution in Component and Functional
projects because their tests share one database. Unit tests keep xUnit's default parallelism. Use the JSON file, not
`[assembly: CollectionBehavior(DisableTestParallelization = true)]`, which is a compile error in xunit.v3 4.x.

**{App}.Tests.AppHost.csproj** (Aspire 13.x: the SDK includes `Aspire.Hosting.AppHost`; `{AspireVersion}` is the
Aspire version chosen above)
```xml
<Project Sdk="Aspire.AppHost.Sdk/{AspireVersion}">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <!-- Makes the linked TestHostContract internal here (public in Shared). -->
    <DefineConstants>$(DefineConstants);TEST_APPHOST</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.PostgreSQL" />
    <PackageReference Include="Aspire.Hosting.Kafka" />
    <PackageReference Include="WireMock.Net.Aspire" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="..\..\Orders.Api.Tests.Shared\TestHostContract.cs" Link="TestHostContract.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Orders.Api\Orders.Api.csproj" />
  </ItemGroup>
</Project>
```
Aspire 9.x uses the older shape: `<Project Sdk="Microsoft.NET.Sdk">` + `<Sdk Name="Aspire.AppHost.Sdk" Version="9.x" />`
+ `<IsAspireHost>true</IsAspireHost>` + a `PackageReference` to `Aspire.Hosting.AppHost`. Match the shape of the
repository's existing AppHost if there is one. A plain library referenced by the AppHost that is not a resource
needs `IsAspireProjectResource="false"` on its `ProjectReference`.

## Adding the projects to the solution

```bash
dotnet sln {Solution}.slnx add tests/orchestration/Orders.Api.Tests.AppHost tests/Orders.Api.Tests.Shared \
  tests/Orders.Api.UnitTests tests/Orders.Api.ComponentTests tests/Orders.Api.FunctionalTests --solution-folder tests
```

- Works for `.sln` and, from SDK 9.0.200, `.slnx`. All projects go into one `tests` solution folder.
- Older SDK with `.slnx`: edit the XML by hand; folder names start and end with `/`:
  ```xml
  <Folder Name="/tests/">
    <Project Path="tests/Orders.Api.UnitTests/Orders.Api.UnitTests.csproj" />
  </Folder>
  ```

## The test AppHost

Start from `assets/apphost/AppHost.cs` and `assets/shared/TestHostContract.cs` (placed in Shared, linked into the
AppHost). Rules and why:

- **Only for tests.** Do not reuse the development AppHost: it typically has the dashboard, data volumes, persistent
  containers, cloud resources and ServiceDefaults — all of which make tests slower, stateful or networked.
- **Dashboard disabled explicitly** (`DisableDashboard = true` here and in the fixture) and **no ServiceDefaults or
  OpenTelemetry** in the test environment. The app resource also gets `OTEL_EXPORTER_OTLP_ENDPOINT` set to an empty
  value: apps built on ServiceDefaults enable the OTLP exporter only when it is non-empty, so no telemetry is sent even
  if something injects an endpoint.
- **One AppHost, two modes**, selected by the `TestMode` configuration key passed as an argument
  (`--TestMode=Component|Functional`): component tests need infrastructure only, functional tests also need the apps.
- **Same engines as production**, declared from discovery (external-services.md has the table).
- **No `WithDataVolume`, no `WithLifetime(ContainerLifetime.Persistent)`**: containers live for the session and every
  session starts empty. Do not pin host ports: the testing builder randomises them, which lets component and
  functional test projects run at the same time.
- **Configuration under the app's names** (discovery section 4): `WithReference(db, connectionName: "OrdersDb")` when
  the app's connection-string name differs from the resource name; `WithEnvironment("Section__Key", value)` for
  everything else (endpoints via `resource.GetEndpoint("http")`, connection strings via
  `resource.Resource.ConnectionStringExpression`). Keep the keys in `TestHostContract.AppSettings` so the component
  fixture uses the same ones.
- **Nothing reaches the network**: every URL of a real service is overridden (WireMock, emulator, container) and the
  app gets `HTTP_PROXY`/`HTTPS_PROXY` pointing to a dead local port with `NO_PROXY=localhost,127.0.0.1`, so a missed
  URL fails fast instead of calling the real service.
- **App resources**: `AddProject<Projects.X>(name, launchProfileName: null).WithHttpEndpoint()` — no launch-profile
  ports or environment, HTTP only (no dev certificate, HTTPS redirection inactive); explicit environment name;
  `WithHttpHealthCheck("/health")` only when the app maps that endpoint in the chosen environment (ServiceDefaults
  maps it in Development only; a check that never passes stalls the fixture until its timeout); `WaitFor` every dependency;
  `WithExplicitStart()` so the functional fixture starts the app after the schema, topics and identity stubs exist.
- Verify each API against the Aspire version in use (`WithExplicitStart`, `ResourceCommands` and
  `WithReference(..., connectionName)` are recent additions); adapt rather than downgrade the design.

## tests/README.md

Fill `assets/tests-README.md`: what each project tests, requirements (Docker/Podman only for component/functional),
how to run each type (VSTest or MTP syntax, whichever the repo uses), first-run image pulls, troubleshooting.

## Build checkpoints

Build project by project so errors stay small: UnitTests (independent) → AppHost → Shared → ComponentTests →
FunctionalTests → whole solution. A project builds only once all of its templates are in place: Shared needs the
attributes, auth, database, data, helpers and fixtures together, because the fixture base uses all of them. Run unit
tests as soon as they exist; run component/functional tests once their fixture compiles.
