using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>Smoke coverage of the Todo endpoints through the complete ASP.NET Core pipeline.</summary>
public class TodoEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithValidRequest_PersistsTodoAndCreatedBy()
    {
        // Arrange
        var title = TestData.Unique("create");

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title, description = "Created by a component test" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/Todo/{body!.Id}", response.Headers.Location.ToString(), StringComparison.Ordinal);

        var stored = await Fixture.Todos.FindByTitleAsync(title);
        Assert.NotNull(stored);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored!.CreatedBy);
        Assert.Equal((int)ETodoStatus.Pending, stored.Status);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = "" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithDuplicateTitle_ReturnsBadRequest()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(new TodoBuilder().Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = existing.Title }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_WithExistingTodo_ReturnsOkWithTitle()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.GetAsync($"/api/Todo/{existing.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(existing.Title, body!.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_WithUnknownId_ReturnsNoContent()
    {
        // Act
        var response = await Client.GetAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithExistingTodo_PatchesFieldsAndUpdatedBy()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{existing.Id}",
            new { description = "Updated by a component test", status = (int)ETodoStatus.Completed },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Todos.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal("Updated by a component test", stored!.Description);
        Assert.Equal((int)ETodoStatus.Completed, stored.Status);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored.UpdatedBy);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{Ulid.NewUlid()}", new { description = "missing" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task Delete_WithExistingTodo_RemovesRow()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoOperator)).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/Todo/{existing.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await Fixture.Todos.FindAsync(existing.Id));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithSeededRows_ReturnsThem()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        await Fixture.Todos.AddAsync(new TodoBuilder().CreatedBy(createdBy).Build());
        await Fixture.Todos.AddAsync(new TodoBuilder().CreatedBy(createdBy).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TodoResponse>>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithSeededRow_ReturnsProjection()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/titles", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TodoTitleResponse>>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(body!, item => item.Title == existing.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithOverlongTitle_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = new string('a', 256) }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithOverlongDescription_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = TestData.Unique("long-description"), description = new string('a', 1001) }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithNewTitle_ReplacesIt()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());
        var newTitle = TestData.Unique("renamed");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{existing.Id}", new { title = newTitle }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Todos.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal(newTitle, stored!.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithTitleAlreadyUsedByAnotherTodo_ReturnsBadRequest()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        await Fixture.Todos.AddAsync(new TodoBuilder().CreatedBy(createdBy).Build());
        var target = await Fixture.Todos.AddAsync(new TodoBuilder().CreatedBy(createdBy).Build());
        var taken = await Fixture.Todos.AddAsync(new TodoBuilder().CreatedBy(createdBy).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{target.Id}", new { title = taken.Title }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithOverlongDescription_ReturnsBadRequest()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{existing.Id}", new { description = new string('a', 1001) }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
