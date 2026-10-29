using Microsoft.Extensions.Configuration;

namespace CleanWebApiTemplate.Infrastructure.Secrets;

/// <summary>
/// Exposes the entries of one KV v2 secret as configuration keys. The read happens once, while the host builds its
/// configuration: there is no watcher, no lease to renew and no reload (restart to pick up a rotated secret).
/// </summary>
public sealed class OpenBaoConfigurationProvider : ConfigurationProvider
{
    private readonly OpenBaoClient _client;
    private readonly string _secretPath;

    /// <summary>Provider over a client the caller owns. The source creates one client per process.</summary>
    public OpenBaoConfigurationProvider(OpenBaoClient client, string secretPath)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretPath);

        _client = client;
        _secretPath = secretPath;
    }

    public override void Load()
    {
        // IConfigurationProvider.Load is synchronous by contract. This is one request during startup, before the
        // request pipeline exists, so blocking here cannot deadlock the application.
        var values = _client.ReadAsync(_secretPath, CancellationToken.None).GetAwaiter().GetResult();

        Data = new Dictionary<string, string?>(values.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            Data[key] = value;
        }
    }
}
