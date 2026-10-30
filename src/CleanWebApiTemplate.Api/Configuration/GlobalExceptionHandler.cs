using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanWebApiTemplate.Api.Configuration;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        if (exception is null || httpContext.Response.HasStarted)
            return false;

        // Model binding failures (malformed JSON, a missing required member, a body that is too
        // large) are client errors. They must not be reported as 500.
        if (exception is BadHttpRequestException badRequest)
        {
            int statusCode = badRequest.StatusCode is >= 400 and < 500
                ? badRequest.StatusCode
                : StatusCodes.Status400BadRequest;
            string detail = statusCode == StatusCodes.Status413PayloadTooLarge
                ? "The request body is too large."
                : "The request is invalid.";

            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Client request rejected with status {StatusCode}.", statusCode);

            await ProblemDetailsFactory.WriteAsync(httpContext,
                                                   ProblemDetailsFactory.Create(httpContext, statusCode, detail),
                                                   cancellationToken);
            return true;
        }

        if (exception is JsonException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Client request contained invalid JSON.");

            await ProblemDetailsFactory.WriteAsync(
                httpContext,
                ProblemDetailsFactory.Create(httpContext,
                                             StatusCodes.Status400BadRequest,
                                             "The request is invalid."),
                cancellationToken);
            return true;
        }

        ProblemDetails problemDetails = ProblemDetailsFactory.Create(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "An error occurred while processing your request.");

        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(eventId: new(),
                            exception: exception,
                            "Internal server error: {ExceptionMessage}",
                            exception.Message);

        await ProblemDetailsFactory.WriteAsync(httpContext, problemDetails, cancellationToken);
        return true;
    }
}
