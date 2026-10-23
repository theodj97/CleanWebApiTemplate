using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.FunctionalTests.Todo;

/// <summary>
/// Out-of-process proof that the API trusts the locally signed issuer: valid, anonymous, invalid and insufficient
/// identities, with no outbound call (a metadata download would be recorded by the fixture proxy).
/// </summary>
public class AuthenticationTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task CreateTodo_WithValidLocalToken_ReturnsCreatedWithoutOutboundCalls()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("fn-auth") }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }

    [Fact]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("fn-anon") }, CancellationToken);

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
    public async Task CreateTodo_WithInvalidToken_ReturnsUnauthorized(InvalidToken kind)
    {
        // Arrange
        var client = CreateClientWithInvalidToken(kind, TestUser.TodoUser);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("fn-invalid") }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.AuthenticatedWithoutRoles)]
    public async Task CreateTodo_AuthenticatedWithoutRoles_ReturnsForbidden()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("fn-norole") }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
