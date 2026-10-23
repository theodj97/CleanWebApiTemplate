using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using static {{AppHostNamespace}}.AppHostContract;

namespace {{SharedNamespace}}.Auth;

/// <summary>Deliberately invalid tokens for the authentication tests (references/local-jwt-authentication.md).</summary>
public enum InvalidToken
{
    Malformed,
    Expired,
    NotYetValid,
    WrongIssuer,
    WrongAudience,
    SignedWithUnknownKey,
    Unsigned,
}

/// <summary>
/// Issues locally signed JWTs for <see cref="TestUser"/> identities. A new key is created for every test run and never
/// leaves the process: nothing is written to disk, nothing is committed, no identity provider is called.
/// The applications receive only the validation material, through the configuration keys they already read
/// (AppHostContract.JwtTrust): AppHost arguments in functional mode, in-process settings in component mode.
/// </summary>
public sealed class TestTokenIssuer : IDisposable
{
    private readonly RSA? _rsa;
    private readonly JsonWebTokenHandler _handler = new();

    public TestTokenIssuer(string issuer, string algorithm)
    {
        Issuer = issuer;
        Algorithm = algorithm;

        if (algorithm == SecurityAlgorithms.HmacSha256)
        {
            // Text, because applications usually read the key as UTF-8 text (Jwt:Key); its bytes are the HMAC key.
            SigningKeyText = Base64Url(RandomNumberGenerator.GetBytes(64));
            SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKeyText));
        }
        else if (algorithm == SecurityAlgorithms.RsaSha256)
        {
            _rsa = RSA.Create(2048);
            SigningKey = new RsaSecurityKey(_rsa) { KeyId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)) };
            PublicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Use HS256 or RS256.");
        }
    }

    public string Issuer { get; }

    public string Algorithm { get; }

    public SecurityKey SigningKey { get; }

    /// <summary>HS256 only: the key as the applications read it.</summary>
    public string? SigningKeyText { get; }

    /// <summary>RS256 only: the public key (PEM) for applications that read one from configuration.</summary>
    public string? PublicKeyPem { get; }

    /// <summary>Value of one entry of AppHostContract.JwtTrust for this run (component settings).</summary>
    public string Resolve(TrustValue value, string resourceName) => value switch
    {
        TrustValue.Issuer => Issuer,
        TrustValue.Audience => TestAuthSettings.For(resourceName).Audience,
        TrustValue.SigningKeyText => SigningKeyText ?? throw new InvalidOperationException("SigningKeyText needs HS256."),
        TrustValue.SigningKeyBase64 => Convert.ToBase64String(Encoding.UTF8.GetBytes(
            SigningKeyText ?? throw new InvalidOperationException("SigningKeyBase64 needs HS256."))),
        TrustValue.PublicKeyPem => PublicKeyPem ?? throw new InvalidOperationException("PublicKeyPem needs RS256."),
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

        yield return Algorithm == SecurityAlgorithms.HmacSha256
            ? $"--{LocalAuth.ParameterKey(LocalAuth.SigningKeyParameter)}={SigningKeyText}"
            : $"--{LocalAuth.ParameterKey(LocalAuth.PublicKeyParameter)}={PublicKeyPem}";
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
            InvalidToken.Unsigned => CreateToken(definition, api, Issuer, now, now, now.Add(lifetime), signingKey: null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    /// <summary>JWKS with the public key only (RS256), for the loopback identity stub (last resort, see references).</summary>
    public string GetJwksJson()
    {
        var rsa = _rsa ?? throw new InvalidOperationException("A JWKS needs RS256.");
        var publicKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)) { KeyId = SigningKey.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        return JsonSerializer.Serialize(new
        {
            keys = new[] { new { kty = jwk.Kty, use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = jwk.Kid, n = jwk.N, e = jwk.E } },
        });
    }

    /// <summary>
    /// Minimal OpenID Connect discovery document for the loopback identity stub (last resort, see references).
    /// <paramref name="authority"/> is the stub's base URL; the advertised issuer is <see cref="Issuer"/>.
    /// </summary>
    public string GetDiscoveryDocumentJson(string authority)
    {
        var baseUrl = authority.TrimEnd('/');
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["issuer"] = Issuer,
            ["jwks_uri"] = $"{baseUrl}/.well-known/jwks.json",
            ["response_types_supported"] = new[] { "token" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.RsaSha256 },
        });
    }

    public void Dispose() => _rsa?.Dispose();

    private string CreateWithUnknownKey(TestUserDefinition definition, TestApi api, DateTime now)
    {
        using var otherRsa = _rsa is null ? null : RSA.Create(2048);
        SecurityKey unknownKey = otherRsa is null
            ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64))
            : new RsaSecurityKey(otherRsa) { KeyId = SigningKey.KeyId };
        return CreateToken(definition, api, Issuer, now, now, now.Add(TestAuthSettings.TokenLifetime), unknownKey);
    }

    private string CreateToken(
        TestUserDefinition user, TestApi api, string issuer, DateTime issuedAt, DateTime notBefore, DateTime expires, SecurityKey? signingKey)
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
            SigningCredentials = signingKey is null ? null : new SigningCredentials(signingKey, Algorithm),
        });
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
