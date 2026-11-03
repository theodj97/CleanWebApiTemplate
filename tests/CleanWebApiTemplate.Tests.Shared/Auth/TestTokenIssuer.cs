using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using static CleanWebApiTemplate.AppHost.AppHostContract;

namespace CleanWebApiTemplate.Tests.Shared.Auth;

/// <summary>Deliberately invalid tokens for the authentication tests.</summary>
public enum InvalidToken
{
    Malformed,
    Expired,
    NotYetValid,
    WrongIssuer,
    WrongAudience,
    SignedWithUnknownKey,
    WrongAlgorithm,
    Unsigned,
}

/// <summary>
/// Issues locally signed HS256 JWTs for <see cref="TestUser"/> identities. A new key is created for every test run and
/// never leaves the process: nothing is written to disk, nothing is committed, no identity provider is called.
/// The applications receive only the validation material, through the configuration keys they already read
/// (AppHostContract.JwtTrust): AppHost arguments in functional mode, in-process settings in component mode.
/// </summary>
public sealed class TestTokenIssuer : IDisposable
{
    private readonly JsonWebTokenHandler _handler = new();

    public TestTokenIssuer(string issuer, string algorithm)
    {
        if (algorithm != SecurityAlgorithms.HmacSha256)
            throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Local tests use HS256 only.");

        Issuer = issuer;
        Algorithm = algorithm;

        // Text, because the API reads the key as UTF-8 text (API_KEY); its bytes are the HMAC key.
        SigningKeyText = Base64Url(RandomNumberGenerator.GetBytes(64));
        SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKeyText));
    }

    public string Issuer { get; }

    public string Algorithm { get; }

    public SecurityKey SigningKey { get; }

    /// <summary>The key as the API reads it.</summary>
    public string SigningKeyText { get; }

    /// <summary>Value of one entry of AppHostContract.JwtTrust for this run (component settings).</summary>
    public string Resolve(TrustValue value, string resourceName) => value switch
    {
        TrustValue.Issuer => Issuer,
        TrustValue.Audience => TestAuthSettings.For(resourceName).Audience,
        TrustValue.SigningKeyText => SigningKeyText,
        TrustValue.SigningKeyBase64 => Convert.ToBase64String(Encoding.UTF8.GetBytes(SigningKeyText)),
        TrustValue.Empty => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    /// <summary>In-process settings that make <paramref name="resourceName"/> trust this issuer (component tests).</summary>
    public IEnumerable<KeyValuePair<string, string?>> TrustSettings(string resourceName) =>
        JwtTrust(resourceName).Select(entry => new KeyValuePair<string, string?>(entry.Key, Resolve(entry.Value, resourceName)));

    /// <summary>
    /// AppHost arguments with the trust material of this run. DistributedApplicationTestingBuilder runs the AppHost
    /// in the test process, so these values never appear on a command line.
    /// </summary>
    public IEnumerable<string> AppHostArguments()
    {
        yield return $"--{LocalAuth.AlgorithmKey}={Algorithm}";
        yield return $"--{LocalAuth.IssuerKey}={Issuer}";
        foreach (var application in TestAuthSettings.Applications)
        {
            yield return $"--{LocalAuth.AudienceKey(application)}={TestAuthSettings.For(application).Audience}";
        }

        yield return $"--{LocalAuth.ParameterKey(LocalAuth.SigningKeyParameter)}={SigningKeyText}";
    }

    /// <summary>A valid token for <paramref name="user"/>, shaped for the application <paramref name="resourceName"/>.</summary>
    public string CreateToken(TestUser user, string resourceName)
    {
        var now = DateTime.UtcNow;
        return CreateToken(TestUsers.Get(user), TestAuthSettings.For(resourceName), Issuer, now, now.AddMinutes(-5), now.Add(TestAuthSettings.TokenLifetime), SigningKey);
    }

    /// <summary>A token the application must reject with 401. Everything except the named defect is valid.</summary>
    public string CreateInvalidToken(InvalidToken kind, TestUser user, string resourceName)
    {
        var definition = TestUsers.Get(user);
        var api = TestAuthSettings.For(resourceName);
        var now = DateTime.UtcNow;
        var lifetime = TestAuthSettings.TokenLifetime;

        return kind switch
        {
            InvalidToken.Malformed => "not-a-jwt",
            // Beyond JwtBearer's default 5-minute clock skew in both directions.
            InvalidToken.Expired => CreateToken(definition, api, Issuer, now.AddHours(-2), now.AddHours(-2), now.AddHours(-1), SigningKey),
            InvalidToken.NotYetValid => CreateToken(definition, api, Issuer, now, now.AddHours(1), now.AddHours(2), SigningKey),
            InvalidToken.WrongIssuer => CreateToken(definition, api, "urn:untrusted-issuer", now, now, now.Add(lifetime), SigningKey),
            InvalidToken.WrongAudience => CreateToken(definition, api with { Audience = "urn:untrusted-audience" }, Issuer, now, now, now.Add(lifetime), SigningKey),
            InvalidToken.SignedWithUnknownKey => CreateWithUnknownKey(definition, api, now),
            InvalidToken.WrongAlgorithm => CreateToken(definition, api, Issuer, now, now, now.Add(lifetime), SigningKey, SecurityAlgorithms.HmacSha512),
            InvalidToken.Unsigned => CreateToken(definition, api, Issuer, now, now, now.Add(lifetime), signingKey: null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    public void Dispose()
    {
        // The symmetric key is managed memory; nothing unmanaged to release.
    }

    private string CreateWithUnknownKey(TestUserDefinition definition, TestApi api, DateTime now) =>
        CreateToken(definition,
                    api,
                    Issuer,
                    now,
                    now,
                    now.Add(TestAuthSettings.TokenLifetime),
                    new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)));

    private string CreateToken(
        TestUserDefinition user,
        TestApi api,
        string issuer,
        DateTime issuedAt,
        DateTime notBefore,
        DateTime expires,
        SecurityKey? signingKey,
        string? algorithm = null)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Subject,
            ["name"] = user.Name,
        };

        // Emitted with the exact claim types the target application reads; an array also works for a single role.
        if (user.Roles.Count > 0)
        {
            claims[api.RoleClaimType] = user.Roles.ToArray();
        }

        if (user.Scopes.Count > 0 && api.ScopeClaimType is not null)
        {
            claims[api.ScopeClaimType] = string.Join(' ', user.Scopes);
        }

        foreach (var (type, value) in user.AdditionalClaims)
        {
            claims[type] = value;
        }

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = api.Audience,
            Claims = claims,
            IssuedAt = issuedAt,
            NotBefore = notBefore,
            Expires = expires,
            // No credentials produces an unsigned token ("alg": "none"), which the applications must reject.
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(signingKey, algorithm ?? Algorithm),
        });
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
