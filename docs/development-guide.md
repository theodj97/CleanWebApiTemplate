# Development Guide

This guide covers how to set up and work with the Clean Architecture WebAPI Template.

## Prerequisites

Before starting, ensure you have the following installed:

- **.NET SDK 10.0** or later
- **Visual Studio 2022** (or later) or **Visual Studio Code**
- **Git** (for version control)
- **SQLite** (for local development, handled automatically)
- **OpenJDK** or **dotnet-ef tool** (for Entity Framework migrations)

## Initial Setup

### 1. Install the Template

Clone the repository and install the template:

```bash
dotnet new install .
```

### 2. Verify Installation

Check that the template is available:

```bash
dotnet new list
# Look for "CleanWebApiTemplate" in the list
```

### 3. Create a New Project

```bash
dotnet new CleanWebApiTemplate -o {ProjectName}
```

**Note:** Close your IDE if it's still open when creating the project.

## Project Structure Overview

```
{ProjectRoot}/
├── src/
│   ├── {ProjectName}.Host/          # ASP.NET Core application entry point
│   ├── {ProjectName}.Application/   # Business logic, CQRS handlers, validators
│   ├── {ProjectName}.Infrastructure/# Database, external dependencies
│   └── {ProjectName}.Domain/        # Entities, DTOs, interfaces
└── tests/
    └── {ProjectName}.Testing/       # Integration and unit tests
```

## Configuration

### Environment Variables

The following environment variables are available (set in `appsettings.json` or user secrets):

| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment name (Development, Staging, Production) | Yes |
| `ConnectionStrings:Sqlite` | SQLite database connection string | No (default: `./database.db`) |
| `JWT:Issuer` | JWT issuer (for production) | No |
| `JWT:Audience` | JWT audience (for production) | No |
| `API_KEY` | Secret key for JWT signing | Yes in production |

### Configuration Files

**appsettings.json** (base settings):
```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=database.db"
  },
  "AppSettings": {
    "CorsPolicyAllowOrigin": []
  }
}
```

**User Secrets** (Development only):
```bash
dotnet user-secrets init
dotnet user-secrets set --clear "AppSettings:Secret"
```

## Database Setup

### Running Migrations

```bash
# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Add initial migration
dotnet ef migrations add InitialMigration \
  --project {InfrastructureProject} \
  --startup-project {HostProject} \
  -o Migrations

# Apply migrations
dotnet ef database update \
  --project {InfrastructureProject} \
  --startup-project {HostProject}
```

**Example:**
```bash
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add InitialMigration \
  --project .\src\{ProjectName}.Infrastructure \
  --startup-project .\src\{ProjectName}.Host\
  -o Migrations
dotnet ef database update \
  --project .\src\{ProjectName}.Infrastructure \
  --startup-project .\src\{ProjectName}.Host\
```

### Database Schema

The schema is created idempotently during the application's first startup. The `database.db` file is automatically created in the project root.

## Running the Application

### Development Mode

```bash
dotnet run --project src/{ProjectName}.Host
```

The application will start on `http://localhost:5000` by default.

### Production Build

```bash
dotnet publish --configuration Release --self-contained true
```

## Running Tests

### All Tests

```bash
dotnet test
```

### Specific Test Project

```bash
dotnet test tests/{ProjectName}.Testing
```

### Filter by Test Name

```bash
dotnet test --filter "FullyQualifiedName~TodoIntegration"
```

### Run Integration Tests Only

```bash
dotnet test --filter "Category=Integration"
```

## Adding New Features

### 1. Add a New Entity

Add to the **Domain** layer first:

```csharp
// In {ProjectName}.Domain/Models/Entities/
public class ArticleEntity : BaseEntity<Ulid>
{
    public override Ulid Id { get; set; }
    public required string Title { get; set; }
    public DateTime PublishedDate { get; set; }
    // ... other properties
}
```

### 2. Create DTOs

Create transfer objects for API requests and responses:

```csharp
// In {ProjectName}.Domain/Models/Dtos/
public class ArticleDto
{
    public Ulid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
}
```

### 3. Define Repository Interface

```csharp
// In {ProjectName}.Infrastructure/Repositories/Interfaces/
public interface IArticleRepository
{
    Task<ArticleEntity?> GetByIdAsync(Ulid id, CancellationToken ct);
    Task<IReadOnlyList<ArticleEntity>> GetAllAsync(CancellationToken ct);
    Task<ArticleEntity> InsertAsync(ArticleEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ArticleEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(Ulid id, CancellationToken ct);
}
```

### 4. Implement Repository

```csharp
// In {ProjectName}.Infrastructure/Repositories/
public class ArticleRepository : IArticleRepository
{
    private readonly SqliteConnection _connection;
    // ... implementation using parameterized queries
    
    public async Task<ArticleEntity?> GetByIdAsync(Ulid id, CancellationToken ct)
    {
        // Use parameterized queries
        // Handle mapping manually
    }
}
```

### 5. Create Command/Query

For CQRS commands:

```csharp
// In {ProjectName}.Application/CQRS/Messages/Commands/
public record CreateArticleCommand(
    string Title,
    string Content,
    DateTime PublishedDate) : ICommand<Result<ArticleDto?>>;
```

For CQRS queries:

```csharp
// In {ProjectName}.Application/CQRS/Messages/Queries/
public record GetAllArticlesQuery(
    int? pageNumber = null,
    int? pageSize = null) : IQuery<Result<IReadOnlyList<ArticleDto?>>>;
```

### 6. Create Handler

```csharp
// In {ProjectName}.Application/Handlers/Article/Create/
public class CreateArticleCommandHandler : 
    ValidationCommandHandler<CreateArticleCommand, Result<ArticleDto?>>
{
    private readonly IArticleRepository _repository;
    
    protected override async Task<Result<ArticleDto?>> HandleCommandAsync(
        CreateArticleCommand command, 
        CancellationToken cancellationToken)
    {
        var entity = new ArticleEntity
        {
            Title = command.Title,
            // ... other properties
        };

        await _repository.InsertAsync(entity, cancellationToken);
        return Result.Created<ArticleDto?>(entity.ToDto());
    }
}
```

### 7. Add FluentValidation

```csharp
public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
        
        RuleFor(x => x.PublishedDate)
            .NotEmpty().WithMessage("Publication date is required");
    }
}
```

### 8. Configure Endpoint

```csharp
// In {ProjectName}.Host/Rules/Article/
public class ArticleRoutes : BaseApiRouter
{
    protected override void MapGroup(IEndpointRouteBuilder app)
    {
        var group = CreateAuthorizedRouteGroupBuilder(app, [Constants.USER_POLICY]);
        
        group.MapPost("/", async (CreateArticleCommand command, 
                                   ICommandHandler<CreateArticleCommand, Result<ArticleDto?>> handler,
                                   CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToResponse<ArticleDto?>(ArticleDtoResponse);
        }).Produces<ArticleDtoResponse>(201);
        
        // ... other endpoints
    }
}
```

## API Testing

### Using the .http file

Open the project root's `.http` file (create one if missing):

```http
@httpHeaders
Content-Type: application/json
Accept: application/json

@baseUrl = http://localhost:5000

@postTodoResponse = 
POST {{baseUrl}}/api/todos
Content-Type: application/json

{
  "title": "Test Todo",
  "description": "Testing the API"
}

echo postTodoResponse
```

### Using REST Client

Install the REST Client extension in VS Code or use Postman to test endpoints.

## Code Style

### Naming Conventions

| Type | Convention |
|------|------------|
| Entities | `{Name}Entity` |
| DTOs | `{Name}Dto` |
| Commands | Record type `{Action}{Name}Command` |
| Queries | Record type `{Action}{Name}Query` |
| Repositories | `{Name}Repository` |
| Models | `{Name}Model` |
| Responses | `{Name}Response` |
| Routes | `{Name}Routes` |

### File Organization

```
{ProjectName}.Domain/
├── Models/
│   ├── Entities/          # Business entities
│   ├── Dtos/              # Transfer objects
│   └── Constants/         # Domain constants
└── ResultModel/           # Result, Error types
```

```
{ProjectName}.Application/
├── CQRS/
│   ├── Messages/
│   │   ├── Commands/
│   │   └── Queries/
│   └── Decorators/        # Validation decorators
├── Handlers/
│   └── {EntityName}/
│       ├── Create/
│       ├── Update/
│       ├── Delete/
│       └── GetById/
└── Abstractions/          # Interfaces
```

```
{ProjectName}.Infrastructure/
├── Repositories/
│   ├── Interfaces/        # Repository interfaces
│   └── {EntityName}.cs    # Implementations
└── Data/                  # Entity Framework context
```

```
{ProjectName}.Host/
├── Routes/
│   └── {EntityName}/
│       ├── Create/
│       ├── Update/
│       ├── Delete/
│       └── Get/
├── Configuration/         # Service configuration
└── Models/
    └── Responses/         # API response wrappers
```

## Customization Examples

### Adding Filter Support

```csharp
// In {ProjectName}.Host/Rules/Todo/Filter/
public record FilteredTodoRequest(
    string? Title = null,
    int? Status = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null
);

// In handler
public record FilteredTodoQuery(FilteredTodoRequest request) : 
    IQuery<Result<IEnumerable<TodoDto?>>>;
```

### Adding Pagination

```csharp
public record GetTodoTitlesRequest(
    int? PageNumber = 1,
    int? PageSize = 10,
    IEnumerable<KeyValuePair<string, bool>>? SortProperties = null
);
```

### Adding Authorization

```csharp
var adminGroup = CreateAuthorizedRouteGroupBuilder(app, [Constants.ADMIN_POLICY]);
```

## Best Practices

### 1. Dependency Inversion

Always depend on abstractions in inner layers:

```csharp
// WRONG - Application depends on Infrastructure
public class TodoHandler : ITodoHandler
{
    private TodoRepository _repository; // ❌
}

// CORRECT - Application depends on abstraction
public class TodoHandler : ITodoHandler
{
    private readonly ITodoRepository _repository; // ✅
}
```

### 2. Result Pattern Consistency

Always return `Result<T>` from handlers:

```csharp
public async Task<Result<TodoDto?>> Handle(TodoDto request, CancellationToken ct)
{
    try
    {
        // Business logic
        var entity = await _repository.InsertAsync(request.ToEntity(), ct);
        return Result.Created(entity.ToDto());
    }
    catch (Exception ex) when (ShouldIgnore(ex))
    {
        return Result.Failure(new DomainError("Operation failed", ex.Message));
    }
    catch
    {
        return Result.Failure(new DomainError("Operation failed"));
    }
}
```

### 3. Validation Strategy

Use FluentValidation via decorators:

```csharp
public sealed class CreateTodoCommand : ICommand<Result<TodoDto?>>
    // Validators are automatically applied via ValidationCommandHandler decorator
```

### 4. Error Handling

Create specific error types for common scenarios:

```csharp
public record TodoNotFoundError : NotFoundError
{
    public TodoNotFoundError(TodoEntity todo) 
        : base(null, $"Todo not found: {todo.Id}") {}
}
```

### 5. Logging

Use structured logging with minimal context:

```csharp
if (logger.IsEnabled(LogLevel.Information))
{
    logger.LogInformation("Processing {CommandType} for {Id}", 
        command.GetType().Name, command.Id);
}
```

## Debugging Tips

### Enable Detailed Logging

```json
// appsettings.json (Development only)
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Break on Exceptions

In VS, set a breakpoint in the exception handler:

```csharp
app.UseDeveloperExceptionPage(); // Enable developer exception page
```

### Database Inspection

The SQLite database is at `./database.db`. Use tools like:
- **SQLite Studio** (GUI)
- **DB Browser for SQLite** (GUI)
- **psql command-line** (if configured)

## Continuous Integration

### Run Tests in CI

```yml
# .github/workflows/ci.yml
- name: Run Tests
  run: dotnet test --verbosity normal --collect:"XPlat Code Coverage"
  
- name: Publish Coverage Report
  run: dotnet reportgenerator -reports:[testResults] **-targetdir:coverage-report**
```

### Build Configuration

```bash
dotnet build --configuration Release -p:TreatWarningsAsErrors=true
```

## Common Issues

### Issue: NuGet package conflicts

**Solution:** Run `dotnet restore` to resolve package versions from central directory.

### Issue: Database file not created

**Solution:** Ensure ASPNETCORE_ENVIRONMENT=Development and check connection string.

### Issue: Validation decorator not working

**Solution:** Verify FluentValidation is installed in the Application project and validators have the correct generic types.

### Issue: AOT compatibility warnings

**Solution:** Review code for dynamic dispatch, ensure all types are registered explicitly, and use only AOT-friendly APIs.
