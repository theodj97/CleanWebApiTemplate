using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using {App}.Api.Models.Responses.Project;
using {App}.Domain.Models.Enums.Project;
using {App}.Tests.Shared.Attributes;
using {App}.Tests.Shared.Auth;
using {App}.Tests.Shared.Data;

namespace {App}.Api.ComponentTests.Project;

/// <summary>Smoke coverage of the Project endpoints through the complete ASP.NET Core pipeline.</summary>
public class ProjectEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithValidRequest_PersistsProjectAndCreatedBy()
    {
        // Arrange
        string name = TestData.Unique("project");

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/", new { name, description = "Created by a component test" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ProjectResponse? body = await response.Content.ReadFromJsonAsync<ProjectResponse>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/Project/{body!.Id}", response.Headers.Location.ToString(), StringComparison.Ordinal);

        var stored = await Fixture.Projects.FindByNameAsync(name);
        Assert.NotNull(stored);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored!.CreatedBy);
        Assert.Equal((int)EProjectStatus.Draft, stored.Status);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/", new { name = "" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithDuplicateNameForOwner_ReturnsConflict()
    {
        // Arrange
        string name = TestData.Unique("project");
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().WithName(name)
                                .CreatedBy(TestUsers.Email(TestUser.TodoUser))
                                .Build());

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/", new { name }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Get_WithExistingOwnProject_ReturnsOk()
    {
        // Arrange
        var project = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        HttpResponseMessage response = await Client.GetAsync($"/api/Project/{project.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ProjectResponse? body = await response.Content.ReadFromJsonAsync<ProjectResponse>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(project.Name, body!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Get_WithUnknownId_ReturnsNoContent()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync($"/api/Project/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithMatchingName_ReturnsProjects()
    {
        // Arrange
        string marker = TestData.Unique("filter");
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().WithName(marker)
                                .CreatedBy(TestUsers.Email(TestUser.TodoUser))
                                .Build());

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/filter", new { names = new[] { marker } }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IEnumerable<ProjectResponse>? body = await response.Content.ReadFromJsonAsync<IEnumerable<ProjectResponse>>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Single(body!);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithNoMatch_ReturnsNoContent()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/Project/filter", new { names = new[] { "no-such-project" } }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
