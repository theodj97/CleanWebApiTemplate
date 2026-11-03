using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

public sealed class LocalTokenServiceTests
{
    private const string Key = "0123456789abcdef0123456789abcdef"; // gitleaks:allow (clave de test)
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void Create_BuildsDevTokenClaimsAndSignature()
    {
        var auth = new LocalAuth(Key, "https://localhost", "https://localhost:5001", "role", 60);
        var user = new LocalUser("user", "local.user", "local.user@example.com", ["User"], "sub-1");
        var service = new LocalTokenService(auth, new FixedTimeProvider(Now));

        var token = service.Create(user);

        var parts = token.Token.Split('.');
        Assert.Equal(3, parts.Length);

        var header = ReadPart(parts[0]);
        Assert.Equal("HS256", header["alg"]!.GetValue<string>());
        Assert.Equal("JWT", header["typ"]!.GetValue<string>());

        var payload = ReadPart(parts[1]);
        Assert.Equal("https://localhost", payload["iss"]!.GetValue<string>());
        Assert.Equal("https://localhost:5001", payload["aud"]!.GetValue<string>());
        Assert.Equal("sub-1", payload["sub"]!.GetValue<string>());
        Assert.Equal("local.user", payload["name"]!.GetValue<string>());
        Assert.Equal("local.user@example.com", payload["email"]!.GetValue<string>());
        Assert.Equal("User", payload["role"]![0]!.GetValue<string>());
        Assert.Equal(Now.ToUnixTimeSeconds(), payload["iat"]!.GetValue<long>());
        Assert.Equal(Now.ToUnixTimeSeconds(), payload["nbf"]!.GetValue<long>());
        Assert.Equal(Now.AddMinutes(60).ToUnixTimeSeconds(), payload["exp"]!.GetValue<long>());
        Assert.False(string.IsNullOrWhiteSpace(payload["jti"]!.GetValue<string>()));

        var signingInput = $"{parts[0]}.{parts[1]}";
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(signingInput));
        Assert.Equal(expected, Base64UrlDecode(parts[2]));

        Assert.Equal(Now.AddMinutes(60), token.ExpiresAt);
        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal("user", token.Profile);
        Assert.Equal("local.user@example.com", token.Email);
        Assert.Equal(new[] { "User" }, token.Roles);
    }

    [Fact]
    public void Create_OmitsRoleClaimForNoRoleProfile()
    {
        var auth = new LocalAuth(Key, "https://localhost", "https://localhost:5001", "role", 5);
        var user = new LocalUser("no-role", "local.norole", "local.norole@example.com", [], "sub-5");
        var service = new LocalTokenService(auth, new FixedTimeProvider(Now));

        var token = service.Create(user);
        var payload = ReadPart(token.Token.Split('.')[1]);

        Assert.False(payload.ContainsKey("role"));
        Assert.Equal(Now.AddMinutes(5).ToUnixTimeSeconds(), payload["exp"]!.GetValue<long>());
        Assert.Equal(300, token.ExpiresIn);
    }

    private static JsonObject ReadPart(string part) =>
        (JsonObject)JsonNode.Parse(Encoding.UTF8.GetString(Base64UrlDecode(part)))!;

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
