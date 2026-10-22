namespace {{SharedNamespace}}.Auth;

/// <summary>
/// What the application validates in a token. ADAPT from discovery section 5:
/// <list type="bullet">
/// <item><see cref="Issuer"/>: the app's configured ValidIssuer if it pins one; otherwise any stable value (the
/// discovery document served to the app in functional tests advertises this same issuer).</item>
/// <item><see cref="Audience"/>: the app's Audience / ValidAudience(s).</item>
/// <item><see cref="RoleClaimType"/>: the claim name that ends up as the app's RoleClaimType. With MapInboundClaims on
/// (the JwtBearer default) both "role" and "roles" are mapped to ClaimTypes.Role; with it off, use the exact name the
/// app configured (often "roles").</item>
/// </list>
/// </summary>
public static class TestAuthSettings
{
    public const string Issuer = "https://identity.tests.local/";
    public const string Audience = "api://orders";
    public const string RoleClaimType = "roles";

    /// <summary>Far longer than any test session, so a token never expires mid-run.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
}
