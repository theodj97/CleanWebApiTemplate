# Project Structure

This document describes the organization and purpose of each component in the CleanWebApiTemplate.

## Main Solution Structure

```
CleanWebApiTemplate/
├── .template.config/          # Template metadata for `dotnet new`
├── src/                       # Source code
│   ├── CleanWebApiTemplate.Host/      # Web application hosting
│   ├── CleanWebApiTemplate.Application/ # Business logic layer
│   ├── CleanWebApiTemplate.Infrastructure/ # Infrastructure layer
│   └── CleanWebApiTemplate.Domain/     # Domain layer
├── tests/                     # Test projects
│   └── CleanWebApiTemplate.Testing/   # Integration and unit tests
├── docs/                      # Documentation
├── coverage-report/           # Test coverage reports
├── TestResults/               # Test run results
└── database.db                # SQLite database (created on first run)
```

## Layered Architecture

### Domain Layer
**Path:** `src/CleanWebApiTemplate.Domain/`

**Purpose:** Contains business entities and core business logic, independent of any framework or infrastructure.

**Directory Structure:**

```
CleanWebApiTemplate.Domain/
├── CleanWebApiTemplate.Domain.csproj
├── Configuration/              # Entity configurations for EF migrations
│   └── TodoConfiguration.cs
├── Models/
│   ├── BaseEntity.cs          # Abstract base entity with ID
│   ├── Constants/             # Domain constants and enums
│   │   └── ETodoStatus.cs
│   ├── Dtos/                  # Data Transfer Objects
│   │   └── Todo/
│   │       └── TodoDto.cs
│   ├── Entities/              # Domain entities
│   │   └── TodoEntity.cs
│   └── Enums/                 # Domain-specific enums
│       └── Todo/
│           └── ETodoStatus.cs
└── ResultModel/               # Error handling types
    ├── Error.cs               # Abstract error
    ├── BadRequestError.cs
    ├── DomainError.cs
    ├── ConflictError.cs
    ├── ForbiddenError.cs
    ├── IBaseEntity.cs         # Interface for BaseEntity
    ├── IResultFactory.cs      # Factory for failed results
    ├── NotFoundError.cs
    └── Result.cs              # Generic result wrapper
```

**Key Files:**

| File | Purpose |
|------|---------|
| `BaseEntity<TKey>` | Abstract base class for all entities with ID |
| `TodoEntity` | Example entity with all Todo properties |
| `Error.cs` | Abstract error type with concrete implementations |
| `Result<T>` | Generic result type for operation outcomes |

**Responsibilities:**
- Define data models and business rules
- Provide base classes and interfaces for other layers
- No dependencies on .NET Framework or external packages

---

### Application Layer
**Path:** `src/CleanWebApiTemplate.Application/`

**Purpose:** Contains business use cases, command/query handlers, DTOs, and validation logic.

**Directory Structure:**

```
CleanWebApiTemplate.Application/
├── CleanWebApiTemplate.Application.csproj
├── Abstractions/              # Interfaces (contracts)
│   ├── ICommandHandler.cs
│   └── IQueryHandler.cs
├── CQRS/
│   ├── Decorators/           # Decorator patterns for handlers
│   │   ├── ValidationCommandHandler.cs
│   │   ├── ValidationQueryHandler.cs
│   │   └── ValidationRunner.cs
│   ├── Extensions/           # Extension methods for CQRS
│   │   └── QueryHandlerExtensions.cs
│   └── Messages/             # Command and query messages
│       ├── Commands/
│       │   └── Todo/
│       │       ├── CreateTodoCommand.cs
│       │       ├── DeleteTodoCommand.cs
│       │       └── UpdateTodoCommand.cs
│       └── Queries/
│           └── Todo/
│               ├── GetTodoByIdQuery.cs
│               ├── GetTodoTitlesQuery.cs
│               └── FilteredTodoQuery.cs
├── Handlers/                 # Handler implementations
│   └── Todo/
│       ├── Create/
│       │   ├── CreateTodoCommandHandler.cs
│       │   └── TodoValidator.cs
│       ├── Delete/
│       │   └── DeleteTodoCommandHandler.cs
│       ├── Filtered/
│       │   └── FilteredTodoQueryHandler.cs
│       ├── GetById/
│       │   └── GetTodoByIdQueryHandler.cs
│       ├── GetTitles/
│       │   └── GetTodoTitlesQueryHandler.cs
│       └── Update/
│           ├── TodoValidator.cs (Update specific)
│           └── UpdateTodoCommandHandler.cs
└── Properties/               # Project properties
    └──launchSettings.json
```

**Key Files:**

| File | Purpose |
|------|---------|
| `ICommandHandler<T,TResult>` | Interface for command handlers |
| `IQueryHandler<T,TResult>` | Interface for query handlers |
| `ValidationCommandHandler<T>` | Decorator for command validation |
| `ValidationQueryHandler<T>` | Decorator for query validation |
| `{Command|Query}*Handler` | Business logic implementations |
| `{Command|Query}*Validator` | FluentValidation rules |

**Responsibilities:**
- Define business use cases (commands and queries)
- Implement business logic in handlers
- Validate input via FluentValidation
- Convert entities to DTOs for API response

---

### Infrastructure Layer
**Path:** `src/CleanWebApiTemplate.Infrastructure/`

**Purpose:** Provides technical infrastructure, database access, and external dependencies.

**Directory Structure:**

```
CleanWebApiTemplate.Infrastructure/
├── CleanWebApiTemplate.Infrastructure.csproj
├── CleanWebApiTemplate.Infrastructure.csproj.user (optional)
├── Constants/                 # Shared constants
│   └── TodoTable.cs
├── Data/                     # Database context (if using EF)
│   └── AppDbContext.cs (if applicable)
├── Models/                   # Infrastructure-specific models
├── Properties/               # Project properties
└── Repositories/             # Repository implementations
    ├── Interfaces/           # Repository interfaces
    │   └── ITodoRepository.cs
    ├── Repositories/TodoRepository.cs
    └── TodoSortColumns.cs    # Sort column constants
```

**Key Files:**

| File | Purpose |
|------|---------|
| `ITodoRepository` | Interface defining repository operations |
| `TodoRepository` | ADO.NET implementation with SQLite |
| `TodoConfiguration` | EF Core entity configuration |
| `TodoSortColumns` | Constants for sorting columns |

**Responsibilities:**
- Implement repository pattern for data access
- Configure database connections
- Implement data mapping between entities and database
- Provide external service integrations
- Handle CRUD operations

---

### Host Layer
**Path:** `src/CleanWebApiTemplate.Host/`

**Purpose:** ASP.NET Core web application hosting and configuration.

**Directory Structure:**

```
CleanWebApiTemplate.Host/
├── CleanWebApiTemplate.Host.csproj
├── Configuration/            # Service configuration
│   ├── AppSettingsValidator.cs
│   └── GlobalExceptionHandler.cs
├── Controllers/              # API controllers (if any)
├── Extensions/              # Extension methods
│   ├── ApiResultExtensions.cs
│   └── EndpointExtension.cs
├── Helpers/                 # Utility classes
│   └── DevAuthHandler.cs
├── Models/                  # API response models
│   ├── IBaseResponse.cs
│   ├── Interfaces/
│   │   └── IGroupMap.cs
│   └── Responses/
│       └── Todo/
│           ├── TodoResponse.cs
│           └── TodoTitleResponse.cs
├── Properties/              # Project properties
│   └──launchSettings.json
├── Routes/                 # Route definitions
│   └── Todo/
│       ├── Create/
│       │   └── CreateTodoRequest.cs
│       ├── Filter/
│       │   └── FilteredTodoRequest.cs
│       ├── Get/
│       │   ├── GetTodoTitlesRequest.cs
│       │   └── GetTodoByIdRequest.cs
│       └── Update/
│           └── UpdateTodoRequest.cs
└── TodoRoutes.cs            # Todo endpoint mappings
```

**Key Files:**

| File | Purpose |
|------|---------|
| `Program.cs` / `ConfigureApplication` | Application entry point |
| `ConfigureServices` | Service registration extensions |
| `EndpointExtension` | Minimal API route registration |
| `GlobalExceptionHandler` | Centralized error handling |
| `DevAuthHandler` | Development authentication scheme |
| `TodoRoutes` | Todo API endpoint definitions |
| `{Request}Dto` | Request DTOs for each endpoint |
| `{Response}` | API response wrappers |

**Responsibilities:**
- Configure and host the web application
- Register services and middleware
- Map API endpoints
- Handle authentication and authorization
- Manage global exception handling
- Configure response formatting

---

### Testing Layer
**Path:** `tests/CleanWebApiTemplate.Testing/`

**Purpose:** Test suite for integration and unit testing.

**Directory Structure:**

```
CleanWebApiTemplate.Testing/
├── Configuration/            # Test configuration
│   └── TestAuthHandler.cs
├── Extension/                # Test extensions
│   └── RequestExtension.cs
├── FunctionalTests/         # End-to-end functional tests
│   ├── API/
│   │   └── Todo/
│   │       ├── Delete.cs
│   │       ├── Get.cs
│   │       ├── Post.cs
│   │       └── Put.cs
│   ├── Exceptions/
│   │   └── GlobalExceptionHandlerTests.cs
│   └── HealthCheck/
│       └── HealthCheckTests.cs
├── UnitTests/               # Unit tests
│   ├── Configuration/
│   │   └── AppSettingsValidatorTests.cs
│   ├── Extensions/
│   │   └── ApiResultExtensionsTests.cs
│   └── Repositories/
│       └── TodoRepositoryTests.cs
├── TestServerFixtureExtension.cs # Test fixtures
├── TestServerFixture.cs      # Test server fixture
└── {ProjectName}.Testing.csproj
```

**Key Files:**

| File | Purpose |
|------|---------|
| `TestServerFixture` | Test server setup and teardown |
| `TestServerFixtureExtension` | Database operations in tests |
| `FunctionalTests` | End-to-end API tests |
| `UnitTests` | Unit tests for specific components |
| `GlobalExceptionHandlerTests` | Exception handling tests |
| `HealthCheckTests` | Health check verification |

**Test Categories:**
- **Functional Tests**: Full end-to-end tests making HTTP requests
- **Unit Tests**: Isolated tests for specific components
- **Integration Tests**: Tests for database and external integrations

---

## File Naming Conventions

### Layer-Specific Patterns

| Layer | File Pattern | Namespace Pattern |
|-------|--------------|-------------------|
| Domain | `{Name}Entity.cs` | `CleanWebApiTemplate.Domain.*` |
| Application | `{Name}Handler.cs`, `{Name}Query.cs` | `CleanWebApiTemplate.Application.*` |
| Infrastructure | `{Name}Repository.cs` | `CleanWebApiTemplate.Infrastructure.*` |
| Host | `{Name}Routes.cs`, `{Name}Request.cs` | `CleanWebApiTemplate.Host.*` |
| Testing | `{Name}Tests.cs` | `CleanWebApiTemplate.Testing.*` |

### Project Naming

| Layer | Project Name |
|-------|--------------|
| Host | `CleanWebApiTemplate.Host` |
| Application | `CleanWebApiTemplate.Application` |
| Infrastructure | `CleanWebApiTemplate.Infrastructure` |
| Domain | `CleanWebApiTemplate.Domain` |
| Testing | `CleanWebApiTemplate.Testing` |

---

## Package Configurations

### Domain Project
**NuGet Packages:** None (no dependencies except Domain projects)

### Application Project
**Primary Packages:**
- `FluentValidation` - Validation framework

**Project References:**
- `CleanWebApiTemplate.Domain`

### Infrastructure Project
**Primary Packages:**
- `Microsoft.Data.Sqlite` - ADO.NET SQLite access
- `SQLitePCLRaw.bundle_e_sqlite3` - SQLite PCL runtime

**Project References:**
- `CleanWebApiTemplate.Domain`

### Host Project
**Primary Packages:**
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.OpenApi`
- `Swashbuckle.AspNetCore.SwaggerUI`
- `AspNetCore.HealthChecks.Sqlite`
- `Grpc.AspNetCore`

**Project References:**
- `CleanWebApiTemplate.Application`
- `CleanWebApiTemplate.Infrastructure`

### Testing Project
**Primary Packages:**
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.Testing.Platform`
- `Microsoft.Testing.Extensions.CodeCoverage`
- `xunit.v3.mtp-v2`

---

## Central Package Management

The project uses **central package management** via `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="PackageName" Version="x.y.z" />
  </ItemGroup>
</Project>
```

Each project references only the package without version.

---

## Build Configuration Keys

| Build Property | Value | Purpose |
|----------------|-------|---------|
| `TargetFramework` | `net10.0` | Target .NET version |
| `IsAotCompatible` | `true` | Enable Native AOT |
| `TreatWarningsAsErrors` | `true` | Strict build |
| `ImplicitUsings` | `enable` | Enable using directives |
| `Nullable` | `enable` | Enable nullable reference types |
| `PublishAot` | `true` | Enable AOT publishing |
| `EnableAotAnalyzer` | `true` | Enable AOT analyzer |

---

## Entry Points

### Application Entry Point
**File:** `Program.cs` (or `ConfigureApplication.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapControllers();
app.Run();
```

### Test Entry Point
**File:** `TestServerFixture.cs`

The testing layer uses Web Application Factories for integration testing.
