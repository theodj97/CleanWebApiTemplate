using System.Net;
using System.Net.Sockets;

namespace {{SharedNamespace}}.Database;

/// <summary>
/// Safety net for the destructive resetters: they refuse to delete data from a host that is not this machine.
/// The test AppHost publishes its containers on the loopback interface, so a remote host means a misconfigured
/// connection string (a shared database, a staging environment) or an intentional remote Docker daemon.
/// </summary>
/// <remarks>
/// This is an accident guard, not a security boundary: the environment variable below is deliberately explicit so
/// nobody can disable it by accident, but code that really wants to wipe a remote database can still do so.
/// </remarks>
public static class DatabaseResetTarget
{
    /// <summary>
    /// Set to <c>1</c> or <c>true</c> only when Docker runs on another machine (remote <c>DOCKER_HOST</c>, or a CI
    /// agent where the test process cannot reach the container through the loopback interface).
    /// </summary>
    public const string AllowRemoteHostsVariable = "DOTNET_TESTING_ALLOW_REMOTE_DATABASE_RESET";

    private static readonly string[] LocalHosts =
    [
        "localhost",
        "host.docker.internal",
        "host.containers.internal",
        "gateway.docker.internal",
    ];

    /// <summary>Fails unless <paramref name="host"/> is the local machine.</summary>
    public static void EnsureLocal(string host, string resetterName) => EnsureLocal([host], resetterName);

    /// <summary>Fails unless every endpoint is on the local machine (cluster and replica-set endpoints included).</summary>
    public static void EnsureLocal(IEnumerable<string> hosts, string resetterName)
    {
        var distinct = hosts
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinct.Length == 0)
        {
            throw new InvalidOperationException(
                $"'{resetterName}' could not determine the database host, so it refuses to reset it. " +
                "Use a connection string with an explicit host or a client whose endpoints are known.");
        }

        if (IsRemoteAllowed())
        {
            return;
        }

        var remote = distinct.Where(host => !IsLocalHost(host)).ToArray();
        if (remote.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"'{resetterName}' refuses to reset '{string.Join("', '", remote)}': destructive database resets are only " +
            $"allowed against the local Aspire test containers. Fix the connection string, or set " +
            $"{AllowRemoteHostsVariable}=1 (only when Docker really runs on another machine).");
    }

    /// <summary>Overload for StackExchange.Redis endpoints.</summary>
    public static void EnsureLocal(IEnumerable<EndPoint> endpoints, string resetterName) =>
        EnsureLocal(endpoints.Select(HostOf), resetterName);

    private static string HostOf(EndPoint endpoint) => endpoint switch
    {
        DnsEndPoint dns => dns.Host,
        IPEndPoint ip => ip.Address.ToString(),
        UnixDomainSocketEndPoint => "localhost",
        _ => endpoint.ToString() ?? string.Empty,
    };

    private static bool IsLocalHost(string host)
    {
        var value = host.Trim().Trim('[', ']');
        if (LocalHosts.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);
    }

    private static bool IsRemoteAllowed()
    {
        var value = Environment.GetEnvironmentVariable(AllowRemoteHostsVariable);
        return value is "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
