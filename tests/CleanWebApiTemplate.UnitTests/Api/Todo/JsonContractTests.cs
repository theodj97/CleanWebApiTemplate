using System.Text.Json.Serialization.Metadata;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Todo;
using Microsoft.AspNetCore.Mvc;

namespace CleanWebApiTemplate.UnitTests.Api.Todo;

/// <summary>
/// Native AOT has no reflection-based JSON. Every request and response the feature exposes must be in its
/// source-generated context, and the shared ProblemDetails body in the application one.
/// </summary>
public class JsonContractTests
{
    [Fact]
    public void EveryTodoRequestAndResponse_HasSourceGeneratedMetadata()
    {
        // Arrange: contracts follow the Request/Response suffix convention.
        Type[] contracts = [.. typeof(TodoResponse).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(TodoResponse).Namespace)
            .Where(type => type.IsPublic
                           && (type.Name.EndsWith("Request", StringComparison.Ordinal)
                               || type.Name.EndsWith("Response", StringComparison.Ordinal)))];

        // Act & Assert
        Assert.NotEmpty(contracts);
        Assert.All(contracts, type => Assert.NotNull(FindTypeInfo(type)));
    }

    [Fact]
    public void ProblemDetails_HasSourceGeneratedMetadata()
    {
        // Act & Assert
        Assert.NotNull(TodoJsonSerializerContext.Default.GetTypeInfo(typeof(ProblemDetails))
                       ?? AppJsonSerializerContext.Default.GetTypeInfo(typeof(ProblemDetails)));
    }

    private static JsonTypeInfo? FindTypeInfo(Type type) =>
        TodoJsonSerializerContext.Default.GetTypeInfo(type)
        ?? AppJsonSerializerContext.Default.GetTypeInfo(type);
}
