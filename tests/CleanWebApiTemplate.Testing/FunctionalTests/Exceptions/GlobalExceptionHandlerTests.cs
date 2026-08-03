using CleanWebApiTemplate.Host.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanWebApiTemplate.Testing.FunctionalTests.Exceptions;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithNullException_Should_ReturnFalse()
    {
        // This tests the early return when exception is null
        var logger = LoggerFactory.Create(b => { }).CreateLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);

        // Act
        var result = await handler.TryHandleAsync(new DefaultHttpContext(), null!, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryHandleAsync_WithException_WhenLoggingDisabled_Should_ReturnTrueAndSet500()
    {
        // This tests the path where logger.IsEnabled(LogLevel.Error) is false
        var logger = new NullLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(500, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithException_WhenLoggingEnabled_Should_LogErrorAndReturnTrue()
    {
        // This tests the path where logger.IsEnabled(LogLevel.Error) is true
        var logger = new TestLogger();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception message");

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(500, httpContext.Response.StatusCode);
        Assert.True(logger.LogErrorCalled);
    }

    [Fact]
    public async Task TryHandleAsync_Should_WriteProblemDetailsResponse()
    {
        // Arrange
        var logger = new NullLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var exception = new InvalidOperationException("Test error");

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        responseBody.Position = 0;
        var problemDetails = await System.Text.Json.JsonSerializer.DeserializeAsync<ProblemDetails>(
            responseBody,
            cancellationToken: CancellationToken.None);
        Assert.NotNull(problemDetails);
        Assert.Equal("Server Error", problemDetails.Title);
        Assert.Equal(500, problemDetails.Status);
    }

    private class TestLogger : ILogger<GlobalExceptionHandler>
    {
        public bool LogErrorCalled { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                LogErrorCalled = true;
            }
        }
    }
}
