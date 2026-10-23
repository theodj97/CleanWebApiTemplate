using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client;

namespace {{SharedNamespace}}.Http;

/// <summary>
/// Third-party HTTP APIs simulated by the "wiremock" Aspire resource. Each test declares its stubs in Arrange and
/// verifies the calls WireMock received in Assert; TestBase resets stubs and the request log before every test.
/// For anything beyond these helpers use <see cref="Admin"/> directly (e.g. Admin.GetMappingBuilder() fluent API).
/// Verify method names against the installed WireMock.Net.RestClient version.
/// </summary>
public sealed class WireMockTestHelper(IWireMockAdminApi admin)
{
    public IWireMockAdminApi Admin { get; } = admin;

    public async Task ResetAsync()
    {
        await Admin.ResetMappingsAsync();
        await Admin.ResetRequestsAsync();
    }

    public Task StubAsync(MappingModel mapping) => Admin.PostMappingAsync(mapping);

    /// <summary>
    /// Stubs <paramref name="method"/> <paramref name="path"/> with a JSON response. A <paramref name="delay"/> longer
    /// than the app's timeout simulates a hanging provider. Remember that resilience handlers (retries) multiply the
    /// number of requests WireMock receives.
    /// </summary>
    public Task StubJsonAsync(string method, string path, int statusCode, object? body = null, TimeSpan? delay = null) =>
        Admin.PostMappingAsync(new MappingModel
        {
            Guid = Guid.NewGuid(),
            Request = new RequestModel { Methods = [method.ToUpperInvariant()], Path = path },
            Response = new ResponseModel
            {
                StatusCode = statusCode,
                BodyAsJson = body,
                Headers = new Dictionary<string, object> { ["Content-Type"] = "application/json" },
                Delay = delay is null ? null : (int)delay.Value.TotalMilliseconds,
            },
        });

    /// <summary>Requests received for <paramref name="method"/> <paramref name="path"/> since the last reset.</summary>
    public async Task<IReadOnlyList<LogEntryModel>> GetRequestsAsync(string method, string path)
    {
        var requests = await Admin.GetRequestsAsync();
        return requests
            .Where(entry => string.Equals(entry.Request.Method, method, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Request.Path, path, StringComparison.Ordinal))
            .ToList();
    }
}
