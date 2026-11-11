using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Project;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Project;

/// <summary>
/// Visibility rules for the single-PROJECT endpoints: administrators may access every
/// row, while every other role is restricted to the PROJECTs it created.
/// </summary>
public class ProjectOwnershipTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_AsUser_WithAnotherUsersProject_ReturnsNotFound()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.GetAsync($"/api/projects/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task GetById_AsAdmin_WithAnotherUsersProject_ReturnsOk()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.GetAsync($"/api/projects/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(other.Name, body!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Names_AsUser_ReturnsOnlyOwnNames()
    {
        // Arrange
        var own = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/names", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadNamesAsync(response);
        var name = Assert.Single(body);
        Assert.Equal(own.Name, name.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Names_AsAdmin_ReturnsEveryUsersNames()
    {
        // Arrange
        var userProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        var operatorProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoOperator).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/names", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadNamesAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, name => name.Id == userProject.Id);
        Assert.Contains(body, name => name.Id == operatorProject.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_AsUser_WithAnotherUsersProject_ReturnsNotFound()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{other.Id}",
            new { description = "must not be applied" },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await Fixture.Projects.FindAsync(other.Id);
        Assert.NotNull(stored);
        Assert.NotEqual("must not be applied", stored!.Description);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Update_AsAdmin_WithAnotherUsersProject_ReturnsOk()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{other.Id}",
            new { description = "updated by the admin" },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Projects.FindAsync(other.Id);
        Assert.NotNull(stored);
        Assert.Equal("updated by the admin", stored!.Description);
        Assert.Equal(TestUsers.Email(TestUser.TodoAdmin), stored.UpdatedBy);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task Delete_AsOperator_WithAnotherUsersProject_DoesNotDeleteIt()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/projects/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await Fixture.Projects.FindAsync(other.Id));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Delete_AsAdmin_WithAnotherUsersProject_RemovesIt()
    {
        // Arrange
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/projects/{other.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await Fixture.Projects.FindAsync(other.Id));
    }

    private static ProjectBuilder ProjectOwnedBy(TestUser user) =>
        new ProjectBuilder().CreatedBy(TestUsers.Email(user));

    private static async Task<List<ProjectNameResponse>> ReadNamesAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<ProjectNameResponse>>(
            JsonSerializerOptions.Web, TestContext.Current.CancellationToken) ?? [];
}
