namespace CleanWebApiTemplate.Secrets;

/// <summary>Configuration keys the application reads to reach OpenBao, plus their defaults.</summary>
public static class OpenBaoConfigurationKeys
{
    /// <summary>Base address of the server. When empty, the OpenBao configuration source is a no-op.</summary>
    public const string Address = "OpenBao:Address";

    /// <summary>Token sent in <c>X-Vault-Token</c>.</summary>
    public const string Token = "OpenBao:Token";

    /// <summary>KV v2 mount point.</summary>
    public const string Mount = "OpenBao:Mount";

    /// <summary>Path of the secret that holds the application's configuration, e.g. <c>cleanwebapitemplate/local</c>.</summary>
    public const string Path = "OpenBao:Path";

    /// <summary>Timeout of a single request, in seconds.</summary>
    public const string TimeoutSeconds = "OpenBao:TimeoutSeconds";

    public const string DefaultMount = "secret";

    public const int DefaultTimeoutSeconds = 5;
}
