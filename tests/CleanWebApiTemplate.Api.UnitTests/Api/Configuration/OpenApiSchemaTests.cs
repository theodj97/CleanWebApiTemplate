using CleanWebApiTemplate.Api.Configuration;
using Microsoft.OpenApi;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Configuration;

/// <summary>
/// The schema descriptors are a public contract. These tests cover the empty/missing-schema
/// branches that the full document never exercises, plus the metadata each schema must carry.
/// </summary>
public class OpenApiSchemaTests
{
    [Fact]
    public void DescribeSchemas_WithoutComponents_DoesNotThrow()
    {
        CommonOpenApiSchemas.DescribeSchemas(new OpenApiDocument());
        TodoOpenApiSchemas.DescribeSchemas(new OpenApiDocument());
    }

    [Fact]
    public void CommonSchemas_DescribeUlidProblemDetailsAndSortPair()
    {
        // Arrange
        var ulid = new OpenApiSchema();
        var problem = SchemaWithProperties("type", "title", "status", "detail", "instance");
        var sort = SchemaWithProperties("key", "value");
        var document = DocumentWith(("Ulid", ulid),
                                    ("ProblemDetails", problem),
                                    ("KeyValuePairOfstringAndboolean", sort));

        // Act
        CommonOpenApiSchemas.DescribeSchemas(document);

        // Assert
        Assert.Equal(JsonSchemaType.String, ulid.Type);
        Assert.Equal("ulid", ulid.Format);
        Assert.Equal("^[0-9A-HJKMNP-TV-Z]{26}$", ulid.Pattern);
        Assert.Equal("01HWXHRB80000G000000000001", ulid.Example?.GetValue<string>());

        Assert.Equal("Problem type identifier.", Property(problem, "type")!.Description);
        Assert.Equal("HTTP status code.", Property(problem, "status")!.Description);
        Assert.Equal("One sort key and whether its order is descending.", sort.Description);
        Assert.Contains("id, title", Property(sort, "key")!.Description);
    }

    [Fact]
    public void TodoSchemas_DescribeRequestsAndResponses()
    {
        // Arrange
        var create = SchemaWithProperties("title", "description");
        var update = SchemaWithProperties("title", "description", "status");
        var filter = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["ids"] = ArraySchema(),
                ["title"] = ArraySchema(),
                ["status"] = ArraySchema(),
                ["createdBy"] = ArraySchema(),
                ["startDate"] = new OpenApiSchema(),
                ["endDate"] = new OpenApiSchema(),
                ["pageNumber"] = new OpenApiSchema(),
                ["pageSize"] = new OpenApiSchema(),
                ["sortProperties"] = new OpenApiSchema(),
            },
        };
        var titles = SchemaWithProperties("pageNumber", "pageSize", "sortProperties");
        var response = SchemaWithProperties("id", "title", "description", "createdAt", "status", "createdBy", "updatedBy");
        var titleResponse = SchemaWithProperties("id", "title");
        var document = DocumentWith(("CreateTodoRequest", create),
                                    ("UpdateTodoRequest", update),
                                    ("FilteredTodoRequest", filter),
                                    ("GetTodoTitlesRequest", titles),
                                    ("TodoResponse", response),
                                    ("TodoTitleResponse", titleResponse));

        // Act
        TodoOpenApiSchemas.DescribeSchemas(document);

        // Assert
        Assert.Equal(255, Property(create, "title")!.MaxLength);
        Assert.Equal(1000, Property(create, "description")!.MaxLength);
        Assert.Equal("Prepare the release notes", Property(create, "title")!.Example?.GetValue<string>());

        Assert.Equal(1, Property(update, "status")!.Example?.GetValue<int>());
        Assert.Null(Property(update, "status")!.Enum);

        Assert.Equal("TODO ids to include.", ArrayItems(Property(filter, "ids"))!.Description);
        Assert.Equal(4, ArrayItems(Property(filter, "status"))!.Enum!.Count);
        Assert.Equal("1", Property(filter, "pageNumber")!.Minimum);
        Assert.NotNull(Property(filter, "sortProperties")!.Example);

        Assert.Equal("1", Property(titles, "pageSize")!.Minimum);
        Assert.NotNull(Property(titles, "sortProperties")!.Description);

        Assert.Equal("email", Property(response, "createdBy")!.Format);
        Assert.Equal(4, Property(response, "status")!.Enum!.Count);
        Assert.Equal(255, Property(titleResponse, "title")!.MaxLength);
    }

    [Fact]
    public void Helpers_WithMissingProperties_AreNoOps()
    {
        // Arrange
        var empty = new OpenApiSchema();

        // Act
        OpenApiSchemaHelpers.DescribePaging(empty);
        OpenApiSchemaHelpers.DescribeSort(null);
        OpenApiSchemaHelpers.DescribeStatus(null);
        OpenApiSchemaHelpers.Describe(null, "ignored");
        OpenApiSchemaHelpers.Describe(OpenApiSchemaHelpers.Property(empty, "missing"), "ignored");

        // Assert
        Assert.Null(OpenApiSchemaHelpers.Property(empty, "missing"));
        Assert.Null(OpenApiSchemaHelpers.ArrayItems(null));
        Assert.Null(OpenApiSchemaHelpers.ArrayItems(empty));
    }

    [Fact]
    public void Helpers_DescribeFormatLengthAndExamples()
    {
        // Arrange
        var schema = new OpenApiSchema();
        var paging = SchemaWithProperties("pageNumber", "pageSize");
        var sort = new OpenApiSchema();
        var status = new OpenApiSchema();

        // Act
        OpenApiSchemaHelpers.Describe(schema, "description", format: "email", maxLength: 255, example: "user@example.com");
        OpenApiSchemaHelpers.DescribePaging(paging);
        OpenApiSchemaHelpers.DescribeSort(sort);
        OpenApiSchemaHelpers.DescribeStatus(status, withAllowedValues: true);

        // Assert
        Assert.Equal("description", schema.Description);
        Assert.Equal("email", schema.Format);
        Assert.Equal(255, schema.MaxLength);
        Assert.Equal("user@example.com", schema.Example?.GetValue<string>());

        Assert.Equal("1", OpenApiSchemaHelpers.Property(paging, "pageNumber")!.Minimum);
        Assert.Equal(10, OpenApiSchemaHelpers.Property(paging, "pageSize")!.Example?.GetValue<int>());
        Assert.Contains("createdAt", sort.Description);
        Assert.NotNull(sort.Example);
        Assert.Equal(4, status.Enum!.Count);
    }

    [Fact]
    public void Patch_OnlyInvokesForExistingSchema()
    {
        // Arrange
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Present"] = new OpenApiSchema(),
        };
        var invoked = false;

        // Act
        OpenApiSchemaHelpers.Patch(schemas, "Missing", _ => invoked = true);
        OpenApiSchemaHelpers.Patch(schemas, "Present", _ => invoked = true);

        // Assert
        Assert.True(invoked);
    }

    private static OpenApiDocument DocumentWith(params (string Name, OpenApiSchema Schema)[] schemas) =>
        new()
        {
            Components = new OpenApiComponents
            {
                Schemas = schemas.ToDictionary(schema => schema.Name,
                                               schema => (IOpenApiSchema)schema.Schema),
            },
        };

    private static OpenApiSchema SchemaWithProperties(params string[] names) =>
        new()
        {
            Properties = names.ToDictionary(name => name, _ => (IOpenApiSchema)new OpenApiSchema()),
        };

    private static OpenApiSchema ArraySchema() =>
        new()
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema(),
        };

    private static OpenApiSchema? Property(OpenApiSchema schema, string name) =>
        OpenApiSchemaHelpers.Property(schema, name);

    private static OpenApiSchema? ArrayItems(OpenApiSchema? schema) =>
        OpenApiSchemaHelpers.ArrayItems(schema);
}
