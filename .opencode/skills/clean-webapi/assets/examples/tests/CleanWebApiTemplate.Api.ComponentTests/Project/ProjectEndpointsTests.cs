using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Project;
using CleanWebApiTemplate.Domain.Project;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Project;

/// <summary>Smoke coverage of the Project endpoints through the complete ASP.NET Core pipeline.</summary>
public class ProjectEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithValidRequest_PersistsProjectAndCreatedBy()
    {
        // Arrange
        var name = TestData.Unique("create");

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name, description = "Created by a component test" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/projects/{body!.Id}", response.Headers.Location.ToString(), StringComparison.Ordinal);

        var stored = await Fixture.Projects.FindByNameAsync(name);
        Assert.NotNull(stored);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored!.CreatedBy);
        Assert.Equal(EProjectStatus.Draft, stored.Status);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name = "" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithDuplicateNameForOwner_ReturnsConflict()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name = existing.Name }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithSameNameForDifferentOwners_ReturnsCreated()
    {
        // Arrange
        var name = TestData.Unique("shared-name");
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoAdmin)).WithName(name).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var stored = await Fixture.Projects.FindByNameAsync(name, TestUsers.Email(TestUser.TodoUser));
        Assert.NotNull(stored);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored!.CreatedBy);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithPaddedName_StoresTrimmedName()
    {
        // Arrange
        var name = TestData.Unique("padded");

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name = $"  {name}  " }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(name, body!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithWhitespaceName_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name = "   " }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_WithExistingProject_ReturnsOkWithName()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.GetAsync($"/api/projects/{existing.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(existing.Name, body!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/projects/{Ulid.NewUlid()}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithExistingProject_PatchesFieldsAndUpdatedBy()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{existing.Id}",
            new { description = "Updated by a component test", status = (int)EProjectStatus.Completed },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Projects.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal("Updated by a component test", stored!.Description);
        Assert.Equal(EProjectStatus.Completed, stored.Status);
        Assert.Equal(TestUsers.Email(TestUser.TodoUser), stored.UpdatedBy);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{Ulid.NewUlid()}", new { description = "missing" }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task Delete_WithExistingProject_RemovesRow()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoOperator)).Build());

        // Act
        var response = await Client.DeleteAsync($"/api/projects/{existing.Id}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await Fixture.Projects.FindAsync(existing.Id));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithSeededRows_ReturnsThem()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        await Fixture.Projects.AddAsync(new ProjectBuilder().CreatedBy(createdBy).Build());
        await Fixture.Projects.AddAsync(new ProjectBuilder().CreatedBy(createdBy).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Names_WithSeededRow_ReturnsProjection()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/names", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ProjectNameResponse>>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(body!, item => item.Name == existing.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithOverlongName_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/", new { name = new string('a', 256) }, CancellationToken);

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
            "/api/projects/", new { name = TestData.Unique("long-description"), description = new string('a', 1001) }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithNewName_ReplacesIt()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());
        var newName = TestData.Unique("renamed");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{existing.Id}", new { name = newName }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Projects.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal(newName, stored!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithNameAlreadyUsedByAnotherProject_ReturnsConflict()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        await Fixture.Projects.AddAsync(new ProjectBuilder().CreatedBy(createdBy).Build());
        var target = await Fixture.Projects.AddAsync(new ProjectBuilder().CreatedBy(createdBy).Build());
        var taken = await Fixture.Projects.AddAsync(new ProjectBuilder().CreatedBy(createdBy).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{target.Id}", new { name = taken.Name }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithNameUsedByAnotherOwner_ReturnsOk()
    {
        // Arrange
        var name = TestData.Unique("other-owner-name");
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoAdmin)).WithName(name).Build());
        var target = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{target.Id}", new { name }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Fixture.Projects.FindAsync(target.Id);
        Assert.NotNull(stored);
        Assert.Equal(name, stored!.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithOverlongDescription_ReturnsBadRequest()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/projects/{existing.Id}", new { description = new string('a', 1001) }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
