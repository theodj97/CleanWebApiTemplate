# Testing

This document describes the testing strategies, approaches, and patterns used in the CleanWebApiTemplate.

## Testing Philosophy

The project follows a layered testing approach:

- **Unit Tests**: Test individual components in isolation
- **Functional Tests**: Test full request-response flows
- **Integration Tests**: Test interactions between multiple components
- **Contract Tests**: Test API contracts and response formats

## Test Configuration

### Package Dependencies

**Testing Packages:**
- `Microsoft.AspNetCore.Mvc.Testing` - Web Application Factory
- `Microsoft.Testing.Platform` - Core test platform
- `Microsoft.Testing.Extensions.CodeCoverage` - Code coverage
- `xunit.v3.mtp-v2` - xUnit test runner

### Test Project Structure

```
tests/CleanWebApiTemplate.Testing/
├── CleanWebApiTemplate.Testing.csproj
├── Configuration/              # Test-specific configuration
│   └── TestAuthHandler.cs
├── Extension/                  # Test helper extensions
│   └── RequestExtension.cs
├── FunctionalTests/           # End-to-end API tests
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
├── UnitTests/                 # Component unit tests
│   ├── Configuration/
│   │   └── AppSettingsValidatorTests.cs
│   ├── Extensions/
│   │   └── ApiResultExtensionsTests.cs
│   └── Repositories/
│       └── TodoRepositoryTests.cs
├── TestServerFixture.cs       # Web test server fixture
└── TestServerFixtureExtension.cs # DB operations for testing
```

---

## Test Server Fixture

The `TestServerFixture` is a critical component for testing that provides:

### HTTP Test Server

```csharp
public class TestServerFixture
{
    public WebApplication Application { get; }
    public HttpClient Client { get; }
    public bool IsRunning { get; }
    
    public Task DisposeAsync() { /* cleanup */ }
}
```

### Database Operations

The fixture extends with methods for database operations:

**Adding Test Entities:**
```csharp
public static async Task<TodoEntity> AddDefaultTodo(this TestServerFixture testServerFixture,
                                                    string title = "defaultTitle",
                                                    string description = "defaultDescription",
                                                    DateTime? createdAt = null,
                                                    int status = 0,
                                                    string createdBy = "defaultCreatedBy")
{
    // Creates entity with ULID
    // Inserts via test repository
}
```

**Retrieving Entities:**
```csharp
public static async Task<TodoEntity?> GetTodo(this TestServerFixture testServerFixture, Ulid id)
{
    // Retrieves via test repository
}
```

**Monotonic ULID Generation:**
```csharp
private static Ulid NewMonotonicUlid()
{
    // Ensures ULIDs are monotonically increasing
    // Critical for consistent SQLite test data
}
```

---

## Functional Tests

### End-to-End API Tests

Functional tests simulate real HTTP requests against a running test server.

**Example: GET Todo by ID**
```csharp
public async Task GetTodo_ReturnsDto_WhenValidId(string title, string userId)
{
    var testClient = await _fixture.GetTestClient();
    var todoId = await _fixture.AddDefaultTodo(title: title, createdBy: userId).Id.ToOidString();

    var result = await testClient.GetAsync($"${todoId}");
    var response = result.Content.ReadFromJsonAsync<TodoResponse>().Result;

    Assert.NotNull(response);
    Assert.False(string.IsNullOrEmpty(response.Id));
    Assert.Equal(title, response.Title);
}
```

**Test Categories:**
- **GET Tests**: Retrieve entities, filter, pagination
- **POST Tests**: Create entities, validation
- **PUT Tests**: Update entities, conflict scenarios
- **DELETE Tests**: Delete entities, soft delete
- **Filter Tests**: Advanced query filters
- **Health Check Tests**: Service availability

### Test Data Patterns

**Setup/Teardown:**
```csharp
[SetUp]
public void SetUp()
{
    _fixture = new TestServerFixture();
    _fixture.Start();
}

[TearDown]
public void TearDown()
{
    _fixture?.DisposeAsync();
}
```

**Test Isolation:**
- Each test creates its own test data
- No shared state between tests
- Clean database after each test run

---

## Unit Tests

### Component Tests

Unit tests isolate components from external dependencies.

**Example: AppSettingsValidator**
```csharp
[Fact]
public async Task ValidateAsync_ReturnsSuccess_WhenSettingsValid()
{
    // Arrange
    var appSettings = new AppSettings { Secret = "test-secret", Value = "test-value" };
    
    // Act
    var result = await AppSettingsValidator.ValidateAsync(appSettings);
    
    // Assert
    Assert.True(result.IsSuccess);
}

[Fact]
public async Task ValidateAsync_ReturnsFailure_WhenSecretMissing()
{
    // Arrange
    var appSettings = new AppSettings { Secret = null, Value = "test-value" };
    
    // Act
    var result = await AppSettingsValidator.ValidateAsync(appSettings);
    
    // Assert
    Assert.False(result.IsSuccess);
    Assert.NotNull(result.Error);
}
```

### Repository Unit Tests

**Example: TodoRepository**
```csharp
[Fact]
public async Task GetByIdAsync_ReturnsEntity_WhenExists()
{
    var context = new InMemoryTestContext();
    var todo = await InsertTestTodo(context);
    
    var repository = new TodoRepository(context);
    var result = await repository.GetByIdAsync(todo.Id);
    
    Assert.NotNull(result);
    Assert.Equal(todo.Id, result.Id);
}
```

### Extension Method Tests

**Example: ApiResultExtensions**
```csharp
[Fact]
public void ToResponse_ReturnsOk_WhenSuccess()
{
    var result = Result.Success<TodoDto?>(new { Id = Guid.NewGuid(), Title = "Test" });
    var response = result.ToResponse<TodoDto?>(TodoResponse);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(response.Value);
}

[Fact]
public void ToResponse_ReturnsBadRequest_WhenValidationFailed()
{
    var error = new BadRequestError("Title is required", "Please provide a title");
    var result = Result.Failure<IBaseResponse>(error);
    var response = result.ToResponse<TodoDto?>(TodoResponse);
    
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

---

## Testing Patterns

### HttpClient Test Client Pattern

```csharp
[Trait("Category", "Functional")]
public async Task GetTodo_ReturnsDto_WhenValid(string title, string description, string userId)
{
    // Arrange
    var userId = await _fixture.AddDefaultTodo(title: "test-user", createdBy: userId);
    var todoId = await _fixture.AddDefaultTodo(title: title, description: description, createdBy: userId);
    
    var testClient = await _fixture.GetTestClient();
    
    // Act
    var result = await testClient.GetAsync($"/api/todos/{todoId.Id.ToOidString()}");
    
    // Assert
    var response = await result.Content.ReadFromJsonAsync<TodoResponse>();
    Assert.Null(response.Error);
    Assert.Equal(todoId.Title, response.Value?.Title);
}
```

### Using Test Extensions

```csharp
using CleanWebApiTemplate.Testing;

[TestContext]
private TestServerFixture _fixture;

[SetUp]
public async Task SetUp()
{
    _fixture = new TestServerFixture();
    await _fixture.Start();
}

[MethodCleanup]
public static async Task Cleanup(string[] _testList)
{
    foreach (var test in _testList)
    {
        if (test.Contains("_fixture"))
        {
            await _fixture.DisposeAsync();
        }
    }
}
```

---

## Global Exception Handler Tests

### Testing Exception Handling

```csharp
[Trait("Category", "Functional")]
public class GlobalExceptionHandlerTests
{
    private WebApplication _app;
    private HttpClient _client;

    [SetUp]
    public async Task SetUp()
    {
        var builder = WebApplication.CreateBuilder();
        _app = builder.Build();
        _client = new TestServer(_app).CreateClient();
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsProblemDetails_ForDomainError()
    {
        // Arrange - Trigger a known exception
        _client.BaseAddress = new Uri(_app.Environment.BaseAddress);
        
        // Act
        var result = await _client.GetAsync("/error-endpoint");
        
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        var problemDetails = await result.Content.ReadFromAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Server Error", problemDetails.Title);
    }
}
```

---

## Health Check Tests

### Service Health Verification

```csharp
[Trait("Category", "Integration")]
public class HealthCheckTests
{
    private TestServerFixture _fixture;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new TestServerFixture();
        await _fixture.Start();
    }

    [Fact]
    public async Task ApiHealthCheck_ReturnsHealthy()
    {
        var result = await _fixture.HttpClient.GetAsync("/health");
        var healthCheckResponse = await result.Content.ReadFromAsync<dynamic[]>();
        
        // Find the api health check
        var apiCheck = healthCheckResponse.FirstOrDefault(h => 
            h?.status == Status.Healthy && h?.name?.EndsWith("api", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.NotNull(apiCheck);
    }

    [Fact]
    public async Task SqliteHealthCheck_ReturnsHealthy_WhenDatabaseExists()
    {
        var result = await _fixture.HttpClient.GetAsync("/health");
        var healthCheckResponse = await result.Content.ReadFromAsync<dynamic[]>();
        
        var sqliteCheck = healthCheckResponse.FirstOrDefault(h => 
            h?.status == Status.Healthy && h?.name?.Contains("sqlite", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.NotNull(sqliteCheck);
    }

    [Fact]
    public async Task SqliteHealthCheck_ReturnsUnhealthy_WhenDatabaseMissing()
    {
        // This tests the scenario where SQL connection check fails
        // Configuration should mark as Unhealthy (not Healthy, not Unknown)
        var result = await _fixture.HttpClient.GetAsync("/health");
        var healthCheckResponse = await result.Content.ReadFromAsync<dynamic[]>();
        
        var sqliteCheck = healthCheckResponse.FirstOrDefault(h => 
            h?.name?.Contains("sqlite", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.NotNull(sqliteCheck);
        Assert.True(sqliteCheck.status != Status.Healthy || 
                   sqliteCheck.status != Status.Unknown);
    }
}
```

---

## Code Coverage

### Running Coverage Reports

```bash
# Install code coverage tool
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"[TestResults].coverage.cobertura.xml" \
               -targetdir:"coverage-report" \
               -reporttypes:Cobertura;Html

# Open HTML report
open coverage-report/index.html
```

### Coverage Targets

**Recommended thresholds:**
- Unit tests: 80%+ code coverage
- Critical paths: 100% code coverage
- API endpoints: 100% route coverage
- Exception handling: 100% coverage

### Coverage by Layer

| Layer | Target |
|-------|--------|
| Domain (Entities) | N/A (no business logic) |
| Domain (Result) | 95%+ |
| Application (Handlers) | 85%+ |
| Application (Validators) | 80%+ |
| Infrastructure (Repositories) | 80%+ |
| Host (Configuration) | 75%+ |

---

## Test Utilities

### Helper Methods

**Database Cleanup:**
```csharp
public static class TestServerFixtureExtension
{
    public static async Task ClearDatabase(this TestServerFixture fixture)
    {
        await fixture.ExecuteRepositoryAsync(async repository =>
        {
            await repository.DeleteAsync(repository.Id);
        });
    }
}
```

**Data Generation:**
```csharp
public static string GenerateRandomString(int length)
{
    const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    Random random = new Random();
    return new string(Enumerable.Repeat(characters, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

**Request Extensions:**
```csharp
public static class RequestExtension
{
    public static async Task<TResponse> GetAsResponse<TResponse>(
        this HttpClient client, string url, CancellationToken ct = default)
    {
        var response = await client.GetAsync(url, ct);
        var content = await response.Content.ReadFromJsonAsync<TResponse>(ct);
        return content!;
    }
}
```

---

## Test Organization

### File Naming Convention

- `{Component}Tests.cs` - Test class for specific component
- `{Feature}Tests.cs` - Tests for a specific feature
- `{Feature}HandlerTests.cs` - Tests for a specific handler

### Test Method Naming

- `{MethodName}_ReturnsExpected_WhenCondition()`
- `{MethodName}_ThrowsException_WhenInvalidCondition()`
- `{MethodName}_UpdatesExpected_WhenValidCondition()`

### Categories

```csharp
[Trait("Category", "Unit")]
[Trait("Category", "Integration")]
[Trait("Category", "Functional")]
[Trait("Category", "Performance")]
```

---

## Running Tests

### All Tests

```bash
dotnet test
```

### Specific Test Project

```bash
dotnet test tests/CleanWebApiTemplate.Testing
```

### Filter by Category

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only
dotnet test --filter "Category=Integration"

# Functional tests only
dotnet test --filter "Category=Functional"

# By class name
dotnet test --filter "FullyQualifiedName~Todo"

# By method name
dotnet test --filter "MethodName~Get_"
```

### Debug Tests

```bash
dotnet test --no-build
```

### Coverage with Tests

```bash
dotnet test --verbosity normal --collect:"XPlat Code Coverage"
```

---

## Continuous Integration

### GitHub Actions Example

```yaml
# .github/workflows/tests.yml

name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test (Unit)
      run: dotnet test --filter "Category=Unit" --verbosity normal
      
    - name: Test (Functional)
      run: dotnet test --filter "Category=Functional" --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Code Coverage
      run: dotnet reportgenerator -reports:"[TestResults].coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Cobertura
      
    - name: Upload Coverage
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: coverage
```

---

## Best Practices

### Do

✅ Use descriptive test names that document expected behavior  
✅ Test the business logic, not implementation details  
✅ Use test fixtures for isolated, repeatable tests  
✅ Test both success and failure scenarios  
✅ Use meaningful assertions  
✅ Keep tests fast and independent  
✅ Document test scenarios with comments when complex  

### Don't

❌ Don't test framework internals (ASP.NET Core, Entity Framework)  
❌ Don't use real databases in unit tests  
❌ Don't test database state transitions  
❌ Don't hardcode values in test data  
❌ Don't test every possible input combination  
❌ Don't skip error handling tests  

### Testing Guidelines

| Question | Guideline |
|----------|-----------|
| Should I use a repository test? | No, test via API instead |
| Should I use a mock for a service? | Only for unit tests, use real service for integration |
| Should I test the Result pattern? | Yes, test all error types |
| Should I test validators? | Yes, test boundary conditions |
| Should I test exception handling? | Yes, test common exception scenarios |

### Testing the Result Pattern

```csharp
[Fact]
public Result_Success_ReturnsSuccess_WhenOperationSucceeds()
{
    var result = Result.Success<string>("test value");
    Assert.True(result.IsSuccess);
    Assert.False(result.IsFailure);
    Assert.Equal("test value", result.Value);
    Assert.Null(result.Error);
}

[Fact]
public Result_Success_ReturnsFailure_WhenUsingFailure()
{
    var error = new NotFoundError("Resource not found");
    var result = Result.Failure(error);
    Assert.False(result.IsSuccess);
    Assert.True(result.IsFailure);
    Assert.Null(result.Value);
    Assert.Equal(error, result.Error);
}

[Fact]
public Result_Success_ReturnsCreated_WhenUsingCreated()
{
    var result = Result.Created(string.Empty, isCreated: true);
    Assert.Equal(HttpStatusCode.Created, result.ToHttpStatusCode());
}
```

### Testing Validators

```csharp
[Fact]
public async Task TodoCommandValidator_ValidatesRequiredTitle()
{
    var validator = new CreateTodoCommandValidator();
    var command = new CreateTodoCommand(Title: null!, Description: "test");
    await validator.ValidateAsync(command, CancellationToken.None);
    
    Assert.True(validator.ValidationErrors.Any());
    Assert.Contains(e => e.ErrorCode == ValidationFailure.ErrorCode("Title is required"));
}

[Fact]
public async Task TodoCommandValidator_RejectsTitleOverMaxLength()
{
    var validator = new CreateTodoCommandValidator();
    var command = new CreateTodoCommand(Title: new string('a', 301), Description: "test");
    await validator.ValidateAsync(command, CancellationToken.None);
    
    Assert.True(validator.ValidationErrors.Any());
}
```

---

## Performance Testing

### Benchmarking Handlers

```csharp
[GlobalCleanup]
public static void Dispose() { }

[Benchmark]
public async Task Handle_GetTodoByIdAsync()
{
    var testClient = await _fixture.GetTestClient();
    await testClient.GetAsync("/api/todos/1", CancellationToken.None);
}
```

### Running Benchmarks

```bash
# Install BenchmarkDotNet
dotnet tool install --global dotnet-benchmark-dotnet
```

---

## Troubleshooting

### Test Fails in CI but Not Locally

**Likely causes:**
- Race conditions
- Environment-specific issues
- Database state not reset

**Solutions:**
```csharp
// Ensure database is always fresh for each test
public async Task SetUp()
{
    await _fixture.DisposeAsync();
    _fixture = new TestServerFixture();
    await _fixture.Start();
}

// Use deterministic test data
[TestData]
public static IEnumerable<(string Title, string Description)> TestData =>
    new List<(string, string)>
    {
        ("First todo", "First description"),
        ("Second todo", "Second description"),
    };

[Theory]
[InlineData("Test Title", "Test Description")]
public async Task TestWithTheory(string title, string description)
{
    // Test is reproducible with same input
}
```

### Slow Tests

**Causes:**
- Database connections not released
- Tests calling services unnecessarily

**Solutions:**
- Use `[Fact]` (single test) instead of `[Theory]` with large datasets
- Mock external dependencies when possible
- Clean up resources promptly

### Test Order Dependencies

Never allow test order dependencies:

```csharp
// ❌ BAD - Test depends on previous test
[Fact]
public async Task TestA() { /* creates data */ }

[Fact]
public async Task TestB() { /* depends on TestA data */ }

// ✅ GOOD - All tests independent
[Fact]
public async Task TestA() { /* creates its own data */ }

[Fact]
public async Task TestB() { /* creates its own data */ }
```

---

## Summary

The project uses a comprehensive testing approach with:

- **xUnit** as the test framework
- **Web Application Factory** for integration tests
- **ADO.NET** for database access in tests
- **Test fixtures** for shared test infrastructure
- **Code coverage** analysis for quality assurance
- **CI/CD integration** for automated testing

**Key Testing Types:**
1. **Unit Tests**: Isolate components for fast feedback
2. **Functional Tests**: Test full request-response flows
3. **Exception Tests**: Verify error handling behavior
4. **Health Check Tests**: Monitor service availability

**Test Quality:**
- High code coverage targets
- Fast, independent tests
- Deterministic behavior
- Clear naming and documentation
