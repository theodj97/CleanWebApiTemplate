using System.Net.Http.Json;
using System.Text.Json;

namespace CleanWebApiTemplate.Infrastructure.Secrets;

/// <summary>
/// Minimal OpenBao (Vault-compatible) client for KV v2 secrets. It is written by hand because the .NET clients
/// available today depend on reflection or Newtonsoft.Json, which breaks the Native AOT publish of the Api.
/// Only what the application needs: read and write one secret.
/// </summary>
public sealed class OpenBaoClient
{
    private const string TokenHeader = "X-Vault-Token";

    private readonly HttpClient _http;
    private readonly Uri _baseAddress;
    private readonly string _mountPath;
    private readonly string _token;

    /// <summary>Creates a client over an <see cref="HttpClient"/> the caller owns and disposes.</summary>
    /// <exception cref="ArgumentException">The address, the token or the mount path is missing or invalid.</exception>
    public OpenBaoClient(HttpClient http, OpenBaoOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);

        if (Uri.TryCreate(options.Address.TrimEnd('/') + "/", UriKind.Absolute, out var address) is false ||
            address.Scheme is not ("http" or "https"))
            throw new ArgumentException($"'{options.Address}' is not an absolute HTTP address.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Token))
            throw new ArgumentException("The OpenBao token is required.", nameof(options));

        _mountPath = options.MountPath.Trim('/');
        if (_mountPath.Length is 0)
            throw new ArgumentException("The OpenBao KV mount path is required.", nameof(options));

        _http = http;
        _baseAddress = address;
        _token = options.Token;
    }

    /// <summary>Reads the string entries of the KV v2 secret at <paramref name="path"/>.</summary>
    /// <exception cref="OpenBaoException">The server returned an error or an unexpected payload.</exception>
    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, path, cancellationToken).ConfigureAwait(false);

        var payload = await response.Content
            .ReadFromJsonAsync(OpenBaoJsonSerializerContext.Default.OpenBaoReadResponse, cancellationToken)
            .ConfigureAwait(false);

        return payload?.Data?.Data
            ?? throw new OpenBaoException($"OpenBao returned an unexpected payload for '{path}': no data.data object.");
    }

    /// <summary>Creates or replaces the KV v2 secret at <paramref name="path"/> with <paramref name="values"/>.</summary>
    /// <exception cref="OpenBaoException">The server returned an error.</exception>
    public async Task WriteAsync(string path, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(values);

        using var request = CreateRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(
            new OpenBaoWriteRequest { Data = new Dictionary<string, string>(values, StringComparer.Ordinal) },
            OpenBaoJsonSerializerContext.Default.OpenBaoWriteRequest);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, path, cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(_baseAddress, $"v1/{_mountPath}/data/{path.TrimStart('/')}"));
        request.Headers.Add(TokenHeader, _token);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var detail = await DescribeFailureAsync(response, cancellationToken).ConfigureAwait(false);
        throw new OpenBaoException(
            $"OpenBao returned {(int)response.StatusCode} ({response.ReasonPhrase}) for '{path}'{detail}.");
    }

    // Vault-compatible servers answer errors with {"errors":["..."]}. The body is never included raw: only that list,
    // which carries the reason and never the token or a secret value.
    private static async Task<string> DescribeFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync(OpenBaoJsonSerializerContext.Default.OpenBaoErrorResponse, cancellationToken)
                .ConfigureAwait(false);

            if (error?.Errors is { Count: > 0 } errors) return ": " + string.Join("; ", errors);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Not the standard error payload: the status code already says what happened.
        }

        return string.Empty;
    }
}
