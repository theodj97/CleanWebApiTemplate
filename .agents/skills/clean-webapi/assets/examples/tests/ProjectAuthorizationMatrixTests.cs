using System.Net;
using System.Net.Http.Headers;
using System.Text;
using {App}.Domain.Models.Entities;
using {App}.Tests.Shared.Attributes;
using {App}.Tests.Shared.Auth;
using {App}.Tests.Shared.Data;

namespace {App}.Api.ComponentTests.Project;

/// <summary>
/// The user group is shared by GET, create and filter; every one of them must enforce
/// the same 401/403 boundary, not only create.
/// </summary>
public class ProjectAuthorizationMatrixTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Theory]
    [InlineData("GET", "/api/Project/{id}", null)]
    [InlineData("POST", "/api/Project/filter", "{}")]
    [InlineData("POST", "/api/Project/", """{"name":"matrix-create"}""")]
    [ResetDatabase]
    public async Task UserGroup_Anonymous_ReturnsUnauthorized(string method, string path, string? body)
    {
        ProjectEntity existing = await SeedProjectAsync();

        HttpResponseMessage response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Project/{id}", null)]
    [InlineData("POST", "/api/Project/filter", "{}")]
    [InlineData("POST", "/api/Project/", """{"name":"matrix-create"}""")]
    [AuthenticatedAs(TestUser.AuthenticatedWithoutRoles)]
    [ResetDatabase]
    public async Task UserGroup_WithoutRoles_ReturnsForbidden(string method, string path, string? body)
    {
        ProjectEntity existing = await SeedProjectAsync();

        HttpResponseMessage response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Project/{id}", null)]
    [InlineData("POST", "/api/Project/filter", "{}")]
    [InlineData("POST", "/api/Project/", """{"name":"matrix-create"}""")]
    [AuthenticatedAs(TestUser.TodoExternal)]
    [ResetDatabase]
    public async Task UserGroup_AsExternal_ReturnsForbidden(string method, string path, string? body)
    {
        ProjectEntity existing = await SeedProjectAsync();

        HttpResponseMessage response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Project/{id}", null, HttpStatusCode.OK)]
    [InlineData("POST", "/api/Project/filter", "{}", HttpStatusCode.OK)]
    [InlineData("POST", "/api/Project/", """{"name":"matrix-create"}""", HttpStatusCode.Created)]
    [AuthenticatedAs(TestUser.TodoOperator)]
    [ResetDatabase]
    public async Task UserGroup_AsOperator_ReturnsSuccess(string method,
                                                           string path,
                                                           string? body,
                                                           HttpStatusCode expected)
    {
        ProjectEntity existing = await SeedProjectAsync();

        HttpResponseMessage response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(expected, response.StatusCode);
    }

    private async Task<ProjectEntity> SeedProjectAsync() =>
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoOperator)).Build());

    private async Task<HttpResponseMessage> SendAsync(string method,
                                                      string path,
                                                      string? body,
                                                      Ulid id)
    {
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            path.Replace("{id}", id.ToString(), StringComparison.Ordinal));

        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await Client.SendAsync(request, CancellationToken);
    }
}
