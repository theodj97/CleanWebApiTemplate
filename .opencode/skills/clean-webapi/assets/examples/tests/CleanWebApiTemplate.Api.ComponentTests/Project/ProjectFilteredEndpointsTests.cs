using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Project;
using CleanWebApiTemplate.Domain.Project;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Project;

/// <summary>
/// Filtering, ordering and pagination against the real PostgreSQL container. These tests exercise the SQL built by
/// <c>ProjectRepository</c> (IN lists, AND conditions, date range, ORDER BY and LIMIT/OFFSET), not only the validators.
/// </summary>
public class ProjectFilteredEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithSeveralIds_ReturnsOnlyTheSelectedProjects()
    {
        // Arrange
        var first = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        var second = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { ids = new[] { first.Id.ToString(), second.Id.ToString() } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadProjectsAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, project => project.Id == first.Id);
        Assert.Contains(body, project => project.Id == second.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithNameFragment_ReturnsMatchingProjects()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        var first = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(createdBy).WithName(TestData.Unique("alpha")).Build());
        var second = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(createdBy).WithName(TestData.Unique("alpha")).Build());
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(createdBy).WithName(TestData.Unique("beta")).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { names = new[] { "alpha" } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadProjectsAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, project => project.Id == first.Id);
        Assert.Contains(body, project => project.Id == second.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithWhitespaceName_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { names = new[] { "   " } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithLikeWildcard_TreatsItLiterally()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        var percent = await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(createdBy).WithName("batch3-100% done").Build());
        await Fixture.Projects.AddAsync(
            new ProjectBuilder().CreatedBy(createdBy).WithName("batch3-100X done").Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { names = new[] { "100%" } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(percent.Id, project.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithEveryCriteria_ReturnsOnlyTheMatchingProject()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        var matching = await Fixture.Projects.AddAsync(new ProjectBuilder()
            .CreatedBy(createdBy)
            .WithStatus(EProjectStatus.Active)
            .Build());
        var other = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        var request = new
        {
            ids = new[] { matching.Id.ToString(), other.Id.ToString() },
            names = new[] { matching.Name },
            statuses = new[] { (int)EProjectStatus.Active },
            owners = new[] { createdBy },
            startDate = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            endDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/filter", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(matching.Id, project.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithDateRangeOutsideTheProject_ReturnsEmptyArray()
    {
        // Arrange
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new
            {
                startDate = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                endDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithLocalOffsetDateRange_ReturnsTheProject()
    {
        // Arrange
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new
            {
                startDate = DateTime.Now.AddHours(-1).ToString("o"),
                endDate = DateTime.Now.AddHours(1).ToString("o"),
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(await ReadProjectsAsync(response));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithDescendingNameSort_ReturnsTheHighestNameFirst()
    {
        // Arrange
        var lower = TestData.Unique("aaa-sort");
        var higher = TestData.Unique("zzz-sort");
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(lower).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(higher).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { sortProperties = new[] { new { key = "Name", value = true } } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadProjectsAsync(response);
        Assert.Equal(higher, body[0].Name);
        Assert.Equal(lower, body[1].Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithPagination_ReturnsOnlyTheRequestedPage()
    {
        // Arrange
        var first = TestData.Unique("aaa-page");
        var second = TestData.Unique("bbb-page");
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(first).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(second).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new
            {
                sortProperties = new[] { new { key = "Name", value = false } },
                pageNumber = 2,
                pageSize = 1,
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(second, project.Name);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_AsUser_WithoutOwners_ReturnsOnlyOwnProjects()
    {
        // Arrange
        var own = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(own.Id, project.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_AsUser_WithAnotherUsersOwner_ReturnsOnlyOwnProjects()
    {
        // Arrange
        var own = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { owners = new[] { TestUsers.Email(TestUser.TodoAdmin) } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(own.Id, project.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithoutOwners_ReturnsProjectsFromEveryUser()
    {
        // Arrange
        var userProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        var operatorProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoOperator).Build());
        var adminProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadProjectsAsync(response);
        Assert.Equal(3, body.Count);
        Assert.Contains(body, project => project.Id == userProject.Id);
        Assert.Contains(body, project => project.Id == operatorProject.Id);
        Assert.Contains(body, project => project.Id == adminProject.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithOwner_ReturnsOnlyThatUsersProjects()
    {
        // Arrange
        var userProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoOperator).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { owners = new[] { TestUsers.Email(TestUser.TodoUser) } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = Assert.Single(await ReadProjectsAsync(response));
        Assert.Equal(userProject.Id, project.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithSeveralOwners_ReturnsTheUnionOfTheirProjects()
    {
        // Arrange
        var userProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).Build());
        var operatorProject = await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoOperator).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new
            {
                owners = new[]
                {
                    TestUsers.Email(TestUser.TodoUser),
                    TestUsers.Email(TestUser.TodoOperator),
                },
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadProjectsAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, project => project.Id == userProject.Id);
        Assert.Contains(body, project => project.Id == operatorProject.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Names_WithSortAndPagination_ReturnsOnlyTheRequestedPage()
    {
        // Arrange
        var first = TestData.Unique("aaa-names");
        var second = TestData.Unique("bbb-names");
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(first).Build());
        await Fixture.Projects.AddAsync(ProjectOwnedBy(TestUser.TodoUser).WithName(second).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/names",
            new
            {
                sortProperties = new[] { new { key = "Name", value = false } },
                pageNumber = 2,
                pageSize = 1,
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ProjectNameResponse>>(
            JsonSerializerOptions.Web, CancellationToken);
        var name = Assert.Single(body!);
        Assert.Equal(second, name.Name);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithOnlyOneDateBound_ReturnsBadRequest(bool includeStartDate, bool includeEndDate)
    {
        // Arrange
        var startDate = includeStartDate ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") : null;
        var endDate = includeEndDate ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") : null;

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter", new { startDate, endDate }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithUnknownSortProperty_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter",
            new { sortProperties = new[] { new { key = "Unknown", value = false } } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithPageNumberWithoutPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/filter", new { pageNumber = 1 }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Names_WithPageSizeWithoutPageNumber_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/projects/names", new { pageSize = 1 }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static ProjectBuilder ProjectOwnedBy(TestUser user) =>
        new ProjectBuilder().CreatedBy(TestUsers.Email(user));

    private static async Task<List<ProjectResponse>> ReadProjectsAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonSerializerOptions.Web, TestContext.Current.CancellationToken) ?? [];
}
