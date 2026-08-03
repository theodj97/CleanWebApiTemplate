using System.Security.Claims;
using System.Text.Encodings.Web;
using CleanWebApiTemplate.Host.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Testing.UnitTests.Helpers;

public class DevAuthHandlerTests
{
    [Fact]
    public async Task HandleAuthenticateAsync_Should_ReturnSuccessWithDevUserClaims()
    {
        // Arrange
        var options = new OptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var loggerFactory = NullLoggerFactory.Instance;
        var urlEncoder = UrlEncoder.Default;

        var scheme = new AuthenticationScheme(DevAuthHandler.SCHEME_NAME, DevAuthHandler.SCHEME_NAME, typeof(DevAuthHandler));
        var handler = new TestableDevAuthHandler(options, loggerFactory, urlEncoder);

        var context = new DefaultHttpContext();
        await handler.InitializeAsync(scheme, context);

        // Act
        var result = await handler.TestAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("DevAuthUser", result.Principal!.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Admin", result.Principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("devauth@example.com", result.Principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void SCHEME_NAME_Should_BeDevAuth()
    {
        // Assert
        Assert.Equal("DevAuth", DevAuthHandler.SCHEME_NAME);
    }

    private class OptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private class TestableDevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                                         Microsoft.Extensions.Logging.ILoggerFactory logger,
                                         UrlEncoder encoder) : DevAuthHandler(options, logger, encoder)
    {
        public async Task<AuthenticateResult> TestAuthenticateAsync()
        {
            var method = typeof(DevAuthHandler).GetMethod("HandleAuthenticateAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task<AuthenticateResult>)method!.Invoke(this, null)!;
            return await task;
        }
    }
}
