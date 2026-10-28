using Microsoft.Extensions.Configuration;

namespace CleanWebApiTemplate.Secrets;

/// <summary>Configuration source that reads one KV v2 secret when the configuration is built.</summary>
internal sealed class OpenBaoConfigurationSource : IConfigurationSource
{
    public required OpenBaoOptions Options { get; init; }

    public required string SecretPath { get; init; }

    public required TimeSpan Timeout { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // One client for the process: the read happens at startup, and disposing it here would break a later
        // configuration reload. It holds no connection until the first request.
        var http = new HttpClient { Timeout = Timeout };
        return new OpenBaoConfigurationProvider(new OpenBaoClient(http, Options), SecretPath);
    }
}
