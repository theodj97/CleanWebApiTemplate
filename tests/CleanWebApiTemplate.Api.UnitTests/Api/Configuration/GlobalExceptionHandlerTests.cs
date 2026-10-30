using System.Text.Json;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Configuration;

/// <summary>The global exception handler turns client errors into 400s and everything else into a 500 ProblemDetails.</summary>
public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();

    [Fact]
    public async Task TryHandleAsync_WithNullException_ReturnsFalse()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(_logger);
        var context = new DefaultHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(context, null!, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(handled);
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithException_WritesInternalServerErrorProblemDetails()
    {
        // Arrange
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        var handler = new GlobalExceptionHandler(_logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context,
                                                   new InvalidOperationException("boom"),
                                                   TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", context.Response.ContentType);

        var root = await ReadProblemDetailsAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, root.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal("An error occurred while processing your request.", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_WithBadHttpRequestException_WritesBadRequestProblemDetails()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(_logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context,
                                                   new BadHttpRequestException("invalid body"),
                                                   TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", context.Response.ContentType);

        var root = await ReadProblemDetailsAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("status").GetInt32());
        Assert.Equal("Bad Request", root.GetProperty("title").GetString());
        Assert.Equal("The request is invalid.", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_WithJsonException_WritesBadRequestProblemDetails()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(_logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context,
                                                   new JsonException("invalid json"),
                                                   TestContext.Current.CancellationToken);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var root = await ReadProblemDetailsAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, root.GetProperty("status").GetInt32());
        Assert.Equal("Bad Request", root.GetProperty("title").GetString());
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!));
    }

    private static async Task<JsonElement> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body,
                                                           cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }
}
