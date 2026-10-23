using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace CleanWebApiTemplate.Tests.Shared.Http;

/// <summary>
/// Local HTTP proxy for out-of-process applications (functional mode): records the request line of every outbound
/// call ("CONNECT login.example.com:443 HTTP/1.1") and closes the connection, so the call fails fast and never leaves
/// the machine. Tests assert that <see cref="Requests"/> is empty: a non-fatal call (such as a JwtBearer metadata
/// download that falls back to explicit keys) is caught too, which a dead proxy port alone cannot show.
/// </summary>
public sealed class OutboundCallRecorder : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentQueue<string> _requests = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;

    public OutboundCallRecorder()
    {
        _listener.Start();
        _acceptLoop = AcceptAsync();
    }

    /// <summary>Value for HTTP_PROXY/HTTPS_PROXY of the applications under test.</summary>
    public string ProxyUrl => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    public IReadOnlyCollection<string> Requests => [.. _requests];

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }

        _stop.Dispose();
    }

    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                using var reader = new StreamReader(client.GetStream());
                if (await reader.ReadLineAsync(_stop.Token) is { } requestLine)
                {
                    _requests.Enqueue(requestLine);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                // A client that disconnects early is still a refused call; keep accepting.
            }
        }
    }
}
