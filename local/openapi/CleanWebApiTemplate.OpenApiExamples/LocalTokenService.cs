using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Builds the same HS256 token as local/tools/devtoken.cs: same issuer, audience, role claim
/// and claims, signed with the key the AppHost seeds into OpenBao as API_KEY.
/// </summary>
internal sealed class LocalTokenService(LocalAuth auth, TimeProvider time)
{
    public LocalToken Create(LocalUser user)
    {
        var now = time.GetUtcNow();
        var expires = now.AddMinutes(auth.LifetimeMinutes);

        var payload = new JsonObject
        {
            ["iss"] = auth.Issuer,
            ["aud"] = auth.Audience,
            ["sub"] = user.Subject,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["name"] = user.Name,
            ["email"] = user.Email,
        };

        if (user.Roles.Count > 0)
        {
            var roles = new JsonArray();
            foreach (var role in user.Roles)
            {
                JsonNode? value = JsonValue.Create(role);
                roles.Add(value);
            }

            payload[auth.RoleClaim] = roles;
        }

        var header = new JsonObject
        {
            ["alg"] = LocalAuth.Algorithm,
            ["typ"] = "JWT",
        };

        var signingInput =
            $"{Base64Url(Encoding.UTF8.GetBytes(header.ToJsonString()))}." +
            $"{Base64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()))}";
        var signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(auth.SigningKey),
            Encoding.UTF8.GetBytes(signingInput));

        var token = $"{signingInput}.{Base64Url(signature)}";
        return new LocalToken(token, expires, auth.LifetimeMinutes * 60, user.Profile, user.Email, user.Roles);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
