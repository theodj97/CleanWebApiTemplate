using System.Net;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;

namespace {{FunctionalTestsNamespace}};

// EXAMPLE — the minimal out-of-process authentication set of one application.
/// <summary>
/// The application runs out of process with production authentication code and only environment variables changed
/// by the AppHost. Every outbound HTTP(S) call goes to a recording proxy: the valid-token test also asserts that the
/// application contacted no identity provider (a failed metadata download is not fatal for JwtBearer, so a 200 alone
/// would not prove it).
/// </summary>
public class AuthenticationTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task Get_WithValidLocalToken_ReturnsOkWithoutContactingAnyIdentityProvider()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidToken.Expired)]
    [InlineData(InvalidToken.WrongIssuer)]
    [InlineData(InvalidToken.WrongAudience)]
    [InlineData(InvalidToken.SignedWithUnknownKey)]
    [InlineData(InvalidToken.Unsigned)]
    public async Task Get_WithInvalidToken_ReturnsUnauthorized(InvalidToken kind)
    {
        // Arrange
        var client = CreateClientWithInvalidToken(kind, TestUser.OrdersCustomer);

        // Act
        var response = await client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.AuthenticatedWithoutRoles)]
    public async Task Get_AuthenticatedWithoutPermissions_ReturnsForbidden()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
