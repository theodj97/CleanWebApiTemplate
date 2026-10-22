namespace {{ComponentTestsNamespace}};

/// <summary>
/// Safety net for component tests: an outbound HTTP call to a non-local host throws instead of reaching a real
/// service. The fix is never to relax this handler but to point that dependency to WireMock in the test settings.
/// It covers clients created through IHttpClientFactory; discovery must still override every configured URL.
/// </summary>
public sealed class BlockExternalHttpHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { IsAbsoluteUri: true } uri && !IsLocal(uri))
        {
            throw new InvalidOperationException(
                $"Blocked outbound HTTP call to {uri.GetLeftPart(UriPartial.Path)} during a test. " +
                "Point this dependency to WireMock (or another local substitute) in the test configuration.");
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsLocal(Uri uri) =>
        uri.IsLoopback
        || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
        // Aspire service discovery names ("https+http://catalog", "http://catalog") are resolved to local endpoints
        // by an inner handler, after this one runs.
        || uri.Scheme.Contains('+')
        || !uri.Host.Contains('.');
}
