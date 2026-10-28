using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// Visibility rules for the single-TODO endpoints: administrators may access every
/// row, while every other role is restricted to the TODOs it created.
/// </summary>
public class TodoOwnershipTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_AsUser_WithAnotherUsersTodo_ReturnsNoContent()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.GetAsync($"/api/Todo/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task GetById_AsAdmin_WithAnotherUsersTodo_ReturnsOk()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.GetAsync($"/api/Todo/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(other.Title, body!.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_AsUser_ReturnsOnlyOwnTitles()
    {
        // Arrange
        var own = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/titles", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTitlesAsync(response);
        var title = Assert.Single(body);
        Assert.Equal(own.Title, title.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Titles_AsAdmin_ReturnsEveryUsersTitles()
    {
        // Arrange
        var userTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        var operatorTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoOperator).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/titles", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTitlesAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, title => title.Id == userTodo.Id);
        Assert.Contains(body, title => title.Id == operatorTodo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_AsUser_WithAnotherUsersTodo_ReturnsNotFound()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{other.Id}",
            new { description = "must not be applied" },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await Fixture.Todos.FindAsync(other.Id);
        Assert.NotNull(stored);
        Assert.NotEqual("must not be applied", stored!.Description);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Update_AsAdmin_WithAnotherUsersTodo_ReturnsOk()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{other.Id}",
            new { description = "updated by the admin" },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Todos.FindAsync(other.Id);
        Assert.NotNull(stored);
        Assert.Equal("updated by the admin", stored!.Description);
        Assert.Equal(TestUsers.Email(TestUser.TodoAdmin), stored.UpdatedBy);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task Delete_AsOperator_WithAnotherUsersTodo_DoesNotDeleteIt()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await ReadDeletedAsync(response));
        Assert.NotNull(await Fixture.Todos.FindAsync(other.Id));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Delete_AsAdmin_WithAnotherUsersTodo_RemovesIt()
    {
        // Arrange
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await ReadDeletedAsync(response));
        Assert.Null(await Fixture.Todos.FindAsync(other.Id));
    }

    private static TodoBuilder TodoOwnedBy(TestUser user) =>
        new TodoBuilder().CreatedBy(TestUsers.Email(user));

    private static async Task<List<TodoTitleResponse>> ReadTitlesAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<TodoTitleResponse>>(
            JsonSerializerOptions.Web, TestContext.Current.CancellationToken) ?? [];

    private static async Task<bool> ReadDeletedAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<bool>(
            JsonSerializerOptions.Web, TestContext.Current.CancellationToken);
}
