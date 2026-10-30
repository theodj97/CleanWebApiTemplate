using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanWebApiTemplate.Api.Configuration;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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

            LogClientRequestRejected(statusCode);

            await ProblemDetailsFactory.WriteAsync(httpContext,
                                                   ProblemDetailsFactory.Create(httpContext, statusCode, detail),
                                                   cancellationToken);
            return true;
        }

        if (exception is JsonException)
        {
            LogInvalidJson();

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

        LogInternalServerError(exception, exception.Message);

        await ProblemDetailsFactory.WriteAsync(httpContext, problemDetails, cancellationToken);
        return true;
    }

    [LoggerMessage(EventId = 1,
                   Level = LogLevel.Warning,
                   Message = "Client request rejected with status {StatusCode}.")]
    private partial void LogClientRequestRejected(int statusCode);

    [LoggerMessage(EventId = 2,
                   Level = LogLevel.Warning,
                   Message = "Client request contained invalid JSON.")]
    private partial void LogInvalidJson();

    [LoggerMessage(EventId = 3,
                   Level = LogLevel.Error,
                   Message = "Internal server error: {ExceptionMessage}")]
    private partial void LogInternalServerError(Exception exception, string exceptionMessage);
}
