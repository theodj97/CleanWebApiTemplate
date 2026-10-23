using static {{AppHostNamespace}}.AppHostContract;

namespace {{SharedNamespace}}.Auth;

/// <summary>What one application expects in a token: audience and the claim types it reads roles and scopes from.</summary>
public sealed record TestApi(string Audience, string RoleClaimType, string? ScopeClaimType);

/// <summary>
/// ADAPT from discovery: test-only token settings (the values below are examples). Issuer and audiences are different
/// from local development (local/tools/devtoken.json) and from production whenever the application reads them from
/// configuration, so a token of one environment is never valid in another; values pinned in the application's code are
/// reused here and the per-run key keeps the environments apart.
/// The signing key is not here: <see cref="TestTokenIssuer"/> creates a new one for every test run.
/// </summary>
public static class TestAuthSettings
{
    public const string Issuer = "urn:contoso-shop:tests";

    /// <summary>HS256 because the applications only accept a symmetric key through configuration (discovery).
    /// Use RS256 when every application can receive a public key instead.</summary>
    public const string Algorithm = "HS256";

    /// <summary>Far longer than any test session, so a token never expires mid-run.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    /// <summary>Every application the AppHost makes trust the local issuer (AppHostContract.JwtTrust).</summary>
    public static readonly string[] Applications = [Resources.OrdersApi, Resources.CatalogApi];

    /// <summary>ADAPT: claim names come from each application's code; they are not assumed to be the same everywhere.
    /// With Microsoft.Identity.Web give every user a scope (IDW10201).</summary>
    public static TestApi For(string resourceName) => resourceName switch
    {
        // JwtBearer default mapping: "role" becomes ClaimTypes.Role.
        Resources.OrdersApi => new("urn:contoso-shop:orders-api:tests", RoleClaimType: "role", ScopeClaimType: null),
        // MapInboundClaims = false, RoleClaimType = "roles", scopes in "scp" (space separated).
        Resources.CatalogApi => new("urn:contoso-shop:catalog-api:tests", RoleClaimType: "roles", ScopeClaimType: "scp"),
        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No token settings for this application."),
    };
}
