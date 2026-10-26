using static CleanWebApiTemplate.AppHost.AppHostContract;

namespace CleanWebApiTemplate.Tests.Shared.Auth;

/// <summary>What one application expects in a token: audience and the claim types it reads roles and scopes from.</summary>
public sealed record TestApi(string Audience, string RoleClaimType, string? ScopeClaimType);

/// <summary>
/// Test-only token settings derived from discovery. Issuer and audience are injected into the applications as
/// configuration (<see cref="CleanWebApiTemplate.AppHost.AppHostContract.JwtTrust"/>), so a test never depends on
/// the values written in <c>appsettings</c>. The signing key is not here: <see cref="TestTokenIssuer"/> creates a
/// new one for every test run.
/// </summary>
public static class TestAuthSettings
{
    public const string Issuer = "urn:CleanWebApiTemplate:tests";

    /// <summary>The API only accepts a symmetric key through configuration (<c>API_KEY</c>), so HS256.</summary>
    public const string Algorithm = "HS256";

    /// <summary>Far longer than any test session, so a token never expires mid-run.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    /// <summary>Every application the AppHost makes trust the local issuer (<c>AppHostContract.JwtTrust</c>).</summary>
    public static readonly string[] Applications = [Resources.Api];

    /// <summary>
    /// Claim names come from the API's code: <c>AddJwtBearer</c> keeps the default inbound claim mapping, so the short
    /// <c>role</c> claim becomes <c>ClaimTypes.Role</c> and <c>email</c> becomes <c>ClaimTypes.Email</c>.
    /// The audience is injected into the application as configuration for this run.
    /// </summary>
    public static TestApi For(string resourceName) => resourceName switch
    {
        Resources.Api => new("https://localhost:5001", RoleClaimType: "role", ScopeClaimType: null),
        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No token settings for this application."),
    };
}
