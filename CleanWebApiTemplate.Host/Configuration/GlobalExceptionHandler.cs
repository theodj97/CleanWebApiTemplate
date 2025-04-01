using CleanWebApiTemplate.Domain.ResultModel;
using Microsoft.AspNetCore.Diagnostics;

namespace CleanWebApiTemplate.Host.Configuration;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is null)
            return false;

        ResultModel problemDetails = new()
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Type = "Server Error",
            Detail = "An error occurred while processing your request."
        };

        logger.LogError(eventId: new(), exception: exception, "Internal server error: {exception}", exception.Message);

        httpContext.Response.StatusCode = problemDetails.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails!, cancellationToken);

        return true;
    }
}
