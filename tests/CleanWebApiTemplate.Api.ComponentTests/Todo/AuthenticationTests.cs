using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// Authentication (401) versus authorization (403) on the Todo endpoints, with locally signed tokens only.
/// A 401 for the valid token means the trust wiring is wrong (issuer, audience, key); a 403 means the token is
/// accepted but the role claim type or value does not match.
/// </summary>
public class AuthenticationTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task CreateTodo_WithValidUserToken_ReturnsCreated()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("auth-user"), description = "ok" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("auth-anon") }, CancellationToken);

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
            "/api/Todo/", new { title = TestData.Unique("auth-invalid") }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.AuthenticatedWithoutRoles)]
    public async Task CreateTodo_AuthenticatedWithoutRoles_ReturnsForbidden()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("auth-norole") }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task DeleteTodo_WithUserToken_ReturnsForbidden()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task DeleteTodo_WithOperatorToken_ReturnsOk()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task DeleteTodo_WithAdminToken_ReturnsOk()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoExternal)]
    public async Task DeleteTodo_WithExternalToken_ReturnsForbidden()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
