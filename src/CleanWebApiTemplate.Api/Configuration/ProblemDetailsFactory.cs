using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Builds and writes RFC 9457 problem details with a trace id. Centralised so the exception
/// handler and the status-code pages produce the same contract.
/// </summary>
internal static class ProblemDetailsFactory
{
    public static ProblemDetails Create(HttpContext context, int statusCode, string? detail = null)
    {
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Type = $"https://httpstatuses.io/{statusCode}",
            Detail = detail,
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        return problemDetails;
    }

    public static Task WriteAsync(HttpContext context,
                                  ProblemDetails problemDetails,
                                  CancellationToken cancellationToken)
    {
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problemDetails,
                                                 AppJsonSerializerContext.Default.ProblemDetails,
                                                 contentType: "application/problem+json; charset=utf-8",
                                                 cancellationToken: cancellationToken);
    }
}
