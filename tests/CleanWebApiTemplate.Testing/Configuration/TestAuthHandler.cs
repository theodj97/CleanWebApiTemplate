using System.Security.Claims;
using System.Text.Encodings.Web;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Testing.Configuration;

public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string UserName = "testUser";
    public const string UserEmail = "test@test.tst";
    public static string Role { get; private set; } = string.Empty;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, UserName),
            new Claim(ClaimTypes.Email, UserEmail),
            new Claim(ClaimTypes.Role, Role)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    public static void SetRole(string role)
    {
        if (Constants.AUTHORIZATION_POLICIES.Any(x => x == role) is false) throw new ArgumentException($"Invalid authPolicy: {role}.");

        Role = role;
    }

    public static void ClearRole() => Role = string.Empty;
}
