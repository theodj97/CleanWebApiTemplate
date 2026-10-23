using static CleanWebApiTemplate.AppHost.AppHostContract;

namespace CleanWebApiTemplate.Tests.Shared.Auth;

/// <summary>What one application expects in a token: audience and the claim types it reads roles and scopes from.</summary>
public sealed record TestApi(string Audience, string RoleClaimType, string? ScopeClaimType);

/// <summary>
/// Test-only token settings derived from discovery. The issuer is different from local development
/// (<see cref="CleanWebApiTemplate.AppHost.AppHostContract.LocalAuth"/>/devtoken.json) whenever the application reads it from
/// configuration; the audience is pinned in <c>ConfigureServices</c> and is therefore reused unchanged. The signing key is
/// not here: <see cref="TestTokenIssuer"/> creates a new one for every test run.
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
    /// The audience is the value pinned in <c>ConfigureServices.ConfigureAuth</c>.
    /// </summary>
    public static TestApi For(string resourceName) => resourceName switch
    {
        Resources.Api => new("http://localhost:7210", RoleClaimType: "role", ScopeClaimType: null),
        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No token settings for this application."),
    };
}
