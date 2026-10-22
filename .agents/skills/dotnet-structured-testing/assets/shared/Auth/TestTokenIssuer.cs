using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace {{SharedNamespace}}.Auth;

/// <summary>
/// Issues signed JWTs for <see cref="TestUser"/> identities. The RSA key is created when the test run starts and
/// never leaves the process: no secret is stored in the repository and every run uses a fresh key.
/// How the application is made to trust this issuer depends on the test type (see references/auth.md):
/// component tests configure JwtBearerOptions in-process; functional tests serve <see cref="GetDiscoveryDocumentJson"/>
/// and <see cref="GetJwksJson"/> from the "identity" WireMock resource.
/// </summary>
public sealed class TestTokenIssuer : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly JsonWebTokenHandler _handler = new();

    public TestTokenIssuer(string issuer, string audience)
    {
        Issuer = issuer;
        Audience = audience;
        // A "kid" lets the app pick the right key from the JWKS, exactly as with a real identity provider.
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)) };
    }

    public string Issuer { get; }

    public string Audience { get; }

    /// <summary>
    /// Typed as SecurityKey so the rest of the infrastructure does not depend on the algorithm. For apps that validate
    /// with a symmetric key from configuration (auth.md, option 2) only this class changes: a SymmetricSecurityKey
    /// generated per run, HmacSha256 credentials, and no JWKS.
    /// </summary>
    public SecurityKey SigningKey { get; }

    public string CreateToken(TestUser user) => CreateToken(TestUsers.Get(user));

    public string CreateToken(TestUserDefinition user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Subject,
            ["name"] = user.Name,
            [JwtRegisteredClaimNames.Email] = user.Email,
        };

        // Emitted with the exact claim type the app turns into roles; an array also works for a single role.
        if (user.Roles.Count > 0)
        {
            claims[TestAuthSettings.RoleClaimType] = user.Roles.ToArray();
        }

        foreach (var (type, value) in user.AdditionalClaims)
        {
            claims[type] = value;
        }

        var now = DateTime.UtcNow;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = claims,
            IssuedAt = now,
            // Tolerates small clock differences between the test process, the app process and containers.
            NotBefore = now.AddMinutes(-5),
            Expires = now.Add(TestAuthSettings.TokenLifetime),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <summary>JWKS with the public key only, as served by a real identity provider.</summary>
    public string GetJwksJson()
    {
        var publicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = SigningKey.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);

        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new { kty = jwk.Kty, use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = jwk.Kid, n = jwk.N, e = jwk.E },
            },
        });
    }

    /// <summary>
    /// Minimal OpenID Connect discovery document. <paramref name="authority"/> is the base URL the app uses as
    /// Authority (the identity WireMock endpoint); the advertised issuer is <see cref="Issuer"/>, which must equal the
    /// "iss" of the tokens and any ValidIssuer the app pins.
    /// </summary>
    public string GetDiscoveryDocumentJson(string authority)
    {
        var baseUrl = authority.TrimEnd('/');

        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["issuer"] = Issuer,
            ["jwks_uri"] = $"{baseUrl}/.well-known/jwks.json",
            ["authorization_endpoint"] = $"{baseUrl}/authorize",
            ["token_endpoint"] = $"{baseUrl}/token",
            ["response_types_supported"] = new[] { "code", "id_token", "token" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.RsaSha256 },
        });
    }

    public void Dispose() => _rsa.Dispose();
}
