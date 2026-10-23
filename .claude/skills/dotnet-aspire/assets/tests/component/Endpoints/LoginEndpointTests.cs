using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using {{AppNamespace}};
using {{SharedNamespace}}.Attributes;

namespace {{ComponentTestsNamespace}}.Endpoints;

// EXAMPLE — only for applications that have their own login endpoint.
/// <summary>
/// The application's own login endpoint is a normal endpoint under test. The other tests never use it to obtain
/// tokens: they use locally signed tokens, exactly as for an API without a login endpoint.
/// </summary>
public class LoginEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    public async Task Login_WithValidCredentials_ReturnsTokenTheApiAccepts()
    {
        // Arrange
        var (user, password) = await Fixture.Users.AddAsync(Roles.Customer);
        var login = await Client.PostAsJsonAsync("/auth/login", new LoginRequest(user.UserName, password), CancellationToken);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>(CancellationToken))!.AccessToken;
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var (user, _) = await Fixture.Users.AddAsync(Roles.Customer);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequest(user.UserName, "wrong"), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
