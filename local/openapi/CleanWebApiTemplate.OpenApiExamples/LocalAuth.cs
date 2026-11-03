using System.Text;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Local JWT settings. The signing key is the same text the AppHost seeds into OpenBao as
/// API_KEY; the issuer, audience and role claim are the values the API validates.
/// </summary>
internal sealed class LocalAuth
{
    public const string Algorithm = "HS256";

    public LocalAuth(string signingKey, string issuer, string audience, string roleClaim, int lifetimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("The local JWT signing key must be at least 32 UTF-8 bytes.");
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("The local JWT issuer is required.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("The local JWT audience is required.");
        if (string.IsNullOrWhiteSpace(roleClaim))
            throw new InvalidOperationException("The local JWT role claim is required.");
        if (lifetimeMinutes is < 1 or > 1440)
            throw new InvalidOperationException("The local JWT lifetime must be between 1 and 1440 minutes.");

        SigningKey = signingKey;
        Issuer = issuer;
        Audience = audience;
        RoleClaim = roleClaim;
        LifetimeMinutes = lifetimeMinutes;
    }

    public string SigningKey { get; }

    public string Issuer { get; }

    public string Audience { get; }

    public string RoleClaim { get; }

    public int LifetimeMinutes { get; }
}
