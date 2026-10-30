using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// The user group is shared by GET, create, filter, titles and update; every one of them must
/// enforce the same 401/403 boundary, not only create.
/// </summary>
public class TodoAuthorizationMatrixTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Theory]
    [InlineData("GET", "/api/Todo/{id}", null)]
    [InlineData("POST", "/api/Todo/filter", "{}")]
    [InlineData("POST", "/api/Todo/titles", "{}")]
    [InlineData("PUT", "/api/Todo/{id}", """{"description":"ignored"}""")]
    [ResetDatabase]
    public async Task UserGroup_Anonymous_ReturnsUnauthorized(string method, string path, string? body)
    {
        var existing = await SeedTodoAsync();

        var response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Todo/{id}", null)]
    [InlineData("POST", "/api/Todo/filter", "{}")]
    [InlineData("POST", "/api/Todo/titles", "{}")]
    [InlineData("PUT", "/api/Todo/{id}", """{"description":"ignored"}""")]
    [AuthenticatedAs(TestUser.AuthenticatedWithoutRoles)]
    [ResetDatabase]
    public async Task UserGroup_WithoutRoles_ReturnsForbidden(string method, string path, string? body)
    {
        var existing = await SeedTodoAsync();

        var response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Todo/{id}", null)]
    [InlineData("POST", "/api/Todo/filter", "{}")]
    [InlineData("POST", "/api/Todo/titles", "{}")]
    [InlineData("PUT", "/api/Todo/{id}", """{"description":"ignored"}""")]
    [AuthenticatedAs(TestUser.TodoExternal)]
    [ResetDatabase]
    public async Task UserGroup_AsExternal_ReturnsForbidden(string method, string path, string? body)
    {
        var existing = await SeedTodoAsync();

        var response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Todo/{id}", null)]
    [InlineData("POST", "/api/Todo/filter", "{}")]
    [InlineData("POST", "/api/Todo/titles", "{}")]
    [InlineData("PUT", "/api/Todo/{id}", """{"description":"operator update"}""")]
    [AuthenticatedAs(TestUser.TodoOperator)]
    [ResetDatabase]
    public async Task UserGroup_AsOperator_ReturnsOk(string method, string path, string? body)
    {
        var existing = await SeedTodoAsync();

        var response = await SendAsync(method, path, body, existing.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<TodoEntity> SeedTodoAsync() =>
        await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoOperator)).Build());

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
