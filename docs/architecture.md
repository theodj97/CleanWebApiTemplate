# Architecture

This project follows the Clean Architecture principles with a focus on scalability, maintainability, and separation of concerns.

## Core Principles

### Clean Architecture Layers

The project is organized into four distinct layers, each with specific responsibilities:

#### 1. Domain Layer
The innermost layer containing business entities, domain models, and core business logic.

**Purpose:**
- Defines the core business concepts of the application
- Contains entity classes and domain events
- Defines the Result pattern for error handling
- No dependencies on external frameworks or infrastructure

**Key Components:**
- `BaseEntity<T>` - Base class for all entities
- `TodoEntity` - Example entity implementation
- `Result<T>` - Generic result type encapsulating operation outcomes
- `Error` - Abstract error with specific error types (`BadRequestError`, `DomainError`, `NotFoundError`, `ConflictError`, `UnauthorizedError`, `ForbiddenError`)

#### 2. Application Layer
Contains business use cases, DTOs, and application services.

**Purpose:**
- Implements business logic and use cases
- Handles queries and commands (CQRS)
- Defines data transfer objects (DTOs)
- Contains validation logic via FluentValidation

**Key Components:**
- `ICommandHandler<TCommand, TResult>` / `IQueryHandler<Query, TResult>` - Interfaces for handlers
- `ValidationCommandHandler` / `ValidationQueryHandler` - Decorators for FluentValidation
- `TodoValidator` - Custom validators for commands and queries
- DTOs in `Models/Dtos/` for data transfer between layers

#### 3. Infrastructure Layer
Provides technical implementations and access to external systems.

**Purpose:**
- Implements data access (repositories)
- Configures databases (SQLite with ADO.NET)
- Implements JWT authentication
- Provides external service integrations
- No dependencies from other layers allowed

**Key Components:**
- `ITodoRepository` / `TodoRepository` - Repository interfaces and implementations
- Database context and migrations
- JWT authentication configuration
- `Constants` - Configuration constants

#### 4. Host Layer
The entry point and configuration for the application.

**Purpose:**
- Hosts the web application
- Configures middleware and services
- Maps endpoints and routes
- Handles global exception management

**Key Components:**
- `Program.cs` / `ConfigureApplication` - Application entry point
- `ConfigureServices` - Service registration
- `GlobalExceptionHandler` - Centralized error handling
- `EndpointExtension` - Minimal API routing
- `Models/Responses/` - API response wrappers
- Route groups organized by endpoint (TodoRoutes)

## CQRS Implementation

### Custom CQRS Pattern

The project implements a **MediatR-free CQRS** pattern designed for Native AOT compatibility.

**Query Flow:**
```
HTTP Request → Endpoint → IQueryHandler → Query Execution → Result<T>
```

**Command Flow:**
```
HTTP Request → Endpoint → ICommandHandler → Validation → Command Execution → Result<T>
```

**Key Characteristics:**
- **Explicit DI Registration** - No assembly scanning or reflection
- **Decorator Pattern** - Validation added via `ValidationCommandHandler` / `ValidationQueryHandler`
- **Result Pattern** - All handlers return `Result<T>` for consistent error handling
- **AOT Friendly** - No dynamic dispatch, compile-time type safety

## Result Pattern

The Result pattern encapsulates operation outcomes in a strongly-typed, AOT-friendly manner.

### Core Types

```csharp
// Generic result type
class Result<T>
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error? Error { get; }
    T? Value { get; }
    bool IsNoContent { get; }
    bool IsCreated { get; }
}

// Abstract error with specialized types
abstract record Error(string? Title, string? Description)
{
    record BadRequestError(string title, string description) : Error(title, description);
    record DomainError(string title, string description) : Error(title, description);
    record NotFoundError(string title, string description) : Error(title, description);
    record ConflictError(string title, string description) : Error(title, description);
    record UnauthorizedError() : Error();
    record ForbiddenError() : Error();
}

// Factory interface for AOT compatibility
interface IResultFactory<TResult>
{
    static abstract TResult Failure(Error error);
}
```

### Usage

```csharp
// Success
Result<TodoDto?> result = Result.Success<TodoDto?>(todoDto);

// Failure with error
Result<TodoDto?> result = Result.Failure<IBaseResponse>(new NotFoundError("Todo not found"));

// Created response
Result<TodoDto?> result = Result.Created<TodoDto?>(newTodoDto);
```

## Data Access Layer

### SQLite with ADO.NET

The project uses **pure ADO.NET** for database access, avoiding Entity Framework ORM.

**Benefits:**
- Native AOT compatible
- Full control over queries
- Parameterized queries for security
- Explicit manual mapping

**Repository Pattern:**
```csharp
interface ITodoRepository
{
    Task<TodoEntity?> GetByIdAsync(Ulid id, CancellationToken ct);
    Task<IReadOnlyList<TodoEntity>> SearchAsync(TodoFilter filter, CancellationToken ct);
    Task<TodoEntity> InsertAsync(TodoEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(TodoEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(Ulid id, CancellationToken ct);
}
```

### ULID Identifiers

The project uses **ULID** (Universally Unique Lexicographically Sortable Identifier) for primary keys.

**Benefits:**
- Faster than UUIDs
- Sortable in lexicographic order
- Time-based ordering
- Reduces database fragmentation
- Non-sequential

## Authentication & Authorization

### JWT Authentication (Production)

Using **OpenID Connect / OIDC** for production environments.

**Configuration:**
- Authority: `http://localhost:7210` (example)
- Uses `Microsoft.AspNetCore.Authentication.JwtBearer`
- Valid issuers stored in configuration
- API key in environment variables for signing key

### Dev Authentication

For development, uses a custom `DevAuthHandler` scheme with minimal configuration.

### Authorization Policies

Defined in the host configuration:
- `ADMIN_POLICY` - Administrator access
- `OPERATOR_POLICY` - Operator access (can be user or admin)
- `USER_POLICY` - Regular user access
- `EXTERNAL_POLICY` - External party access

## Global Exception Handling

Centralized exception management via `IExceptionHandler` implementation:

- Catches all unhandled exceptions
- Logs errors with `ILogger`
- Returns standardized `ProblemDetails` response
- Configured via `AddExceptionHandler<GlobalExceptionHandler>()`

## Minimal APIs

The project uses .NET Minimal APIs for route definition:

```csharp
app.MapGet("/{id}", async (string id, IQueryHandler<..., Result<T>> handler, ...) => {
    // Handle request
});
```

**Route Groups:**
- Organized by endpoint type (TodoRoutes, etc.)
- Implement `IGroupMap` interface
- Support authorization filters
- Support endpoint production metadata

## Health Checks

Built-in health check support for:
- API health check (always healthy)
- SQLite database connectivity
- Configurable health status reporting

## Serialization

Custom JSON serialization context (`AppJsonSerializerContext`) for:
- Type information in responses
- Consistent date/time formatting
- AOT-compatible serialization

## Native AOT Optimizations

The project is configured for Native AOT (Ahead-of-Time) compilation:

- `<IsAotCompatible>true</IsAotCompatible>`
- `<EnableAotAnalyzer>true</EnableAotAnalyzer>`
- Dependency injection without reflection
- Explicit type registration
- No dynamic dispatch
- AOT-friendly error handling
