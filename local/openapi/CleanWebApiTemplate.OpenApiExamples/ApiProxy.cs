namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Forwards the Swagger "Try it out" calls to the API, so the browser only talks to this sidecar
/// (one origin, no CORS) and the Authorization header is passed through untouched.
/// </summary>
internal sealed class ApiProxy(HttpClient client, ILogger<ApiProxy> logger)
{
    private static readonly HashSet<string> RequestHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Connection",
        "Keep-Alive",
        "Transfer-Encoding",
        "Upgrade",
        "Proxy-Connection",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Accept-Encoding",
        "Content-Length",
        "Content-Type",
        "Origin",
    };

    private static readonly HashSet<string> ResponseHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "Transfer-Encoding",
        "Connection",
        "Keep-Alive",
        "Upgrade",
        "Proxy-Connection",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Content-Length",
    };

    public async Task ForwardAsync(HttpContext context)
    {
        var target = new Uri(client.BaseAddress!, $"{context.Request.Path}{context.Request.QueryString}");
        logger.LogDebug("Proxying {Method} {Path} to {Target}.", context.Request.Method, context.Request.Path, target);

        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

        foreach (var header in context.Request.Headers)
        {
            if (RequestHeadersToSkip.Contains(header.Key))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            var content = new StreamContent(context.Request.Body);

            if (context.Request.ContentType is { Length: > 0 } contentType)
                content.Headers.TryAddWithoutValidation("Content-Type", contentType);

            if (context.Request.ContentLength is { } contentLength)
                content.Headers.ContentLength = contentLength;

            request.Content = content;
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            if (ResponseHeadersToSkip.Contains(header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            if (ResponseHeadersToSkip.Contains(header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
