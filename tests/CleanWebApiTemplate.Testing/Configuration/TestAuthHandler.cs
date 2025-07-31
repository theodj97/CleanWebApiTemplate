using System.Security.Claims;
using System.Text.Encodings.Web;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Testing.Configuration;

public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                             ILoggerFactory logger,
                             UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    private const string DefaultUserName = "testUser";
    private const string DefaultUserEmail = "test@test.tst";
    private const string DefaultRole = Constants.USER_POLICY;
    public static string UserName { get; private set; } = DefaultUserName;
    public static string UserEmail { get; private set; } = DefaultUserEmail;
    public static string Role { get; private set; } = DefaultRole;

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

    internal static void SetRole(string role)
    {
        if (Constants.AuthorizationPolicies.Any(x => x == role) is false) throw new ArgumentException($"Invalid authPolicy: {role}.");

        Role = role;
    }

    internal static void SetUserName(string userName) => UserName = userName;
    internal static void SetUserEmail(string userEmail) => UserEmail = userEmail;

    internal static void ResetDefault()
    {
        UserName = DefaultUserName;
        UserEmail = DefaultUserEmail;
        Role = DefaultRole;
    }
}
