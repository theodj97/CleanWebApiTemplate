using System.Net;
using System.Text.Json;

namespace CleanWebApiTemplate.Api.ComponentTests;

/// <summary>
/// The OpenAPI document is a contract too: the split schema descriptors must keep the
/// metadata the explorer and the generated clients rely on.
/// </summary>
public class OpenApiContractTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    public async Task Document_DescribesTheApiAndTheBearerScheme()
    {
        // Act
        using var document = await GetDocumentAsync();

        // Assert
        var root = document.RootElement;
        Assert.Equal("CleanWebApiTemplate API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("1.0.0", root.GetProperty("info").GetProperty("version").GetString());

        var tags = root.GetProperty("tags").EnumerateArray().Select(tag => tag.GetProperty("name").GetString());
        Assert.Contains("Todo", tags);

        var securitySchemes = root.GetProperty("components").GetProperty("securitySchemes");
        var bearer = securitySchemes.GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Contains(root.GetProperty("security").EnumerateArray(),
                        requirement => requirement.TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task Document_ContainsEveryTodoOperation()
    {
        // Act
        using var document = await GetDocumentAsync();

        // Assert
        var paths = document.RootElement.GetProperty("paths");
        Assert.All(paths.EnumerateObject().Select(path => path.Name),
                   path => Assert.StartsWith("/api/", path, StringComparison.Ordinal));

        var operationIds = paths.EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(operation => operation.Value.TryGetProperty("operationId", out _))
            .Select(operation => operation.Value.GetProperty("operationId").GetString())
            .OfType<string>()
            .Order()
            .ToArray();

        Assert.Equal(
            ["CreateTodo", "DeleteTodo", "FilterTodos", "GetTodoById", "GetTodoTitles", "UpdateTodo"],
            operationIds);
    }

    [Fact]
    public async Task Operations_DocumentTheStatusCodesTheApiReturns()
    {
        // Act
        using var document = await GetDocumentAsync();

        // Assert
        var paths = document.RootElement.GetProperty("paths");

        AssertResponses(paths.GetProperty("/api/Todo").GetProperty("post"),
                        "201", "400", "401", "403", "409", "500");
        AssertResponses(paths.GetProperty("/api/Todo/{id}").GetProperty("get"),
                        "200", "204", "400", "401", "403", "500");
        AssertResponses(paths.GetProperty("/api/Todo/{id}").GetProperty("put"),
                        "200", "400", "401", "403", "404", "409", "500");
        AssertResponses(paths.GetProperty("/api/Todo/{id}").GetProperty("delete"),
                        "200", "400", "401", "403", "500");
        AssertResponses(paths.GetProperty("/api/Todo/filter").GetProperty("post"),
                        "200", "204", "400", "401", "403", "500");
        AssertResponses(paths.GetProperty("/api/Todo/titles").GetProperty("post"),
                        "200", "204", "400", "401", "403", "500");
    }

    [Fact]
    public async Task Schemas_KeepTheValidationConstraints()
    {
        // Act
        using var document = await GetDocumentAsync();

        // Assert
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var ulid = schemas.GetProperty("Ulid");
        Assert.Equal("string", ulid.GetProperty("type").GetString());
        Assert.Equal("ulid", ulid.GetProperty("format").GetString());
        Assert.Equal("^[0-9A-HJKMNP-TV-Z]{26}$", ulid.GetProperty("pattern").GetString());

        var createSchema = schemas.GetProperty("CreateTodoRequest");
        Assert.Contains("title", createSchema.GetProperty("required").EnumerateArray()
            .Select(property => property.GetString()));

        var create = createSchema.GetProperty("properties");
        Assert.Equal(255, create.GetProperty("title").GetProperty("maxLength").GetInt32());
        Assert.Equal(1000, create.GetProperty("description").GetProperty("maxLength").GetInt32());
        Assert.Equal("Short task description. Trimmed; unique per owner.",
                     create.GetProperty("title").GetProperty("description").GetString());

        var filter = schemas.GetProperty("FilteredTodoRequest").GetProperty("properties");
        Assert.Contains("contains", filter.GetProperty("title").GetProperty("items")
            .GetProperty("description").GetString());

        var todo = schemas.GetProperty("TodoResponse").GetProperty("properties");
        Assert.Equal("#/components/schemas/Ulid", todo.GetProperty("id").GetProperty("$ref").GetString());
        Assert.Equal("email", todo.GetProperty("createdBy").GetProperty("format").GetString());
        Assert.Equal([1, 2, 3, 4], todo.GetProperty("status").GetProperty("enum")
            .EnumerateArray().Select(value => value.GetInt32()).ToArray());

        var problem = schemas.GetProperty("ProblemDetails").GetProperty("properties");
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetProperty("description").GetString()));
    }

    private async Task<JsonDocument> GetDocumentAsync()
    {
        var response = await Client.GetAsync("/openapi/v1.json", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    private static void AssertResponses(JsonElement operation, params string[] expected)
    {
        var responses = operation.GetProperty("responses").EnumerateObject()
            .Select(response => response.Name)
            .ToArray();

        Assert.All(expected, status => Assert.Contains(status, responses));
    }
}
