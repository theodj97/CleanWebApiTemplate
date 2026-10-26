using System.Text.Json;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Configuration;

/// <summary>The global exception handler must turn any unhandled exception into a 500 ProblemDetails response.</summary>
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
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body,
                                                           cancellationToken: TestContext.Current.CancellationToken);
        var root = document.RootElement;
        Assert.Equal(StatusCodes.Status500InternalServerError, root.GetProperty("status").GetInt32());
        Assert.Equal("Server Error", root.GetProperty("title").GetString());
        Assert.Equal("An error occurred while processing your request.", root.GetProperty("detail").GetString());
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!));
    }
}
