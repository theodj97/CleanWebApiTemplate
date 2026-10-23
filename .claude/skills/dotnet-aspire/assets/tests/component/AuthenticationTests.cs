using System.Net;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;

namespace {{ComponentTestsNamespace}};

// EXAMPLE — the authentication matrix of one protected endpoint; one class per protected application.
/// <summary>
/// Authentication (401) versus authorization (403) on one protected endpoint, with locally signed tokens only.
/// A 401 for the valid token means the trust wiring is wrong (issuer, audience, key); a 403 means the token is
/// accepted but the role claim type or value does not match.
/// </summary>
public class AuthenticationTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task GetOrders_WithValidLocalToken_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidToken.Malformed)]
    [InlineData(InvalidToken.Expired)]
    [InlineData(InvalidToken.NotYetValid)]
    [InlineData(InvalidToken.WrongIssuer)]
    [InlineData(InvalidToken.WrongAudience)]
    [InlineData(InvalidToken.SignedWithUnknownKey)]
    [InlineData(InvalidToken.Unsigned)]
    public async Task GetOrders_WithInvalidToken_ReturnsUnauthorized(InvalidToken kind)
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
    public async Task GetOrders_AuthenticatedWithoutRoles_ReturnsForbidden()
    {
        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
