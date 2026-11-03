namespace CleanWebApiTemplate.Infrastructure.Secrets;

/// <summary>
/// How to reach the OpenBao server and which token authenticates the application.
/// Values come from configuration (<c>OpenBao:*</c>); nothing here is committed or defaults to a credential.
/// </summary>
public sealed class OpenBaoOptions
{
    /// <summary>Base address of the server, e.g. <c>http://localhost:8200</c>.</summary>
    public required string Address { get; init; }

    /// <summary>Token sent in <c>X-Vault-Token</c>. Never logged or included in an exception message.</summary>
    public required string Token { get; init; }

    /// <summary>KV v2 mount point.</summary>
    public string MountPath { get; init; } = OpenBaoConfigurationKeys.DefaultMount;
}
