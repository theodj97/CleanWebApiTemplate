using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// Filtering, ordering and pagination against the real PostgreSQL container. These tests exercise the SQL built by
/// <c>TodoRepository</c> (IN lists, AND conditions, date range, ORDER BY and LIMIT/OFFSET), not only the validators.
/// </summary>
public class TodoFilteredEndpointsTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithSeveralIds_ReturnsOnlyTheSelectedTodos()
    {
        // Arrange
        var first = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        var second = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { ids = new[] { first.Id.ToString(), second.Id.ToString() } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTodosAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, todo => todo.Id == first.Id);
        Assert.Contains(body, todo => todo.Id == second.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithTitleFragment_ReturnsMatchingTodos()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        var first = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(createdBy).WithTitle(TestData.Unique("alpha")).Build());
        var second = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(createdBy).WithTitle(TestData.Unique("alpha")).Build());
        await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(createdBy).WithTitle(TestData.Unique("beta")).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { title = new[] { "alpha" } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTodosAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, todo => todo.Id == first.Id);
        Assert.Contains(body, todo => todo.Id == second.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithWhitespaceTitle_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { title = new[] { "   " } },
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
        var percent = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(createdBy).WithTitle("batch3-100% done").Build());
        await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(createdBy).WithTitle("batch3-100X done").Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { title = new[] { "100%" } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(percent.Id, todo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithEveryCriteria_ReturnsOnlyTheMatchingTodo()
    {
        // Arrange
        var createdBy = TestUsers.Email(TestUser.TodoUser);
        var matching = await Fixture.Todos.AddAsync(new TodoBuilder()
            .CreatedBy(createdBy)
            .WithStatus(ETodoStatus.InProgress)
            .Build());
        var other = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        var request = new
        {
            ids = new[] { matching.Id.ToString(), other.Id.ToString() },
            title = new[] { matching.Title },
            status = new[] { (int)ETodoStatus.InProgress },
            createdBy = new[] { createdBy },
            startDate = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            endDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/filter", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(matching.Id, todo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithDateRangeOutsideTheTodo_ReturnsNoContent()
    {
        // Arrange
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new
            {
                startDate = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                endDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithLocalOffsetDateRange_ReturnsTheTodo()
    {
        // Arrange
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new
            {
                startDate = DateTime.Now.AddHours(-1).ToString("o"),
                endDate = DateTime.Now.AddHours(1).ToString("o"),
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(await ReadTodosAsync(response));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithDescendingTitleSort_ReturnsTheHighestTitleFirst()
    {
        // Arrange
        var lower = TestData.Unique("aaa-sort");
        var higher = TestData.Unique("zzz-sort");
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(lower).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(higher).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { sortProperties = new[] { new { key = "Title", value = true } } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTodosAsync(response);
        Assert.Equal(higher, body[0].Title);
        Assert.Equal(lower, body[1].Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithPagination_ReturnsOnlyTheRequestedPage()
    {
        // Arrange
        var first = TestData.Unique("aaa-page");
        var second = TestData.Unique("bbb-page");
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(first).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(second).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new
            {
                sortProperties = new[] { new { key = "Title", value = false } },
                pageNumber = 2,
                pageSize = 1,
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(second, todo.Title);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_AsUser_WithoutCreatedBy_ReturnsOnlyOwnTodos()
    {
        // Arrange
        var own = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(own.Id, todo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_AsUser_WithAnotherUsersCreatedBy_ReturnsOnlyOwnTodos()
    {
        // Arrange
        var own = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { createdBy = new[] { TestUsers.Email(TestUser.TodoAdmin) } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(own.Id, todo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithoutCreatedBy_ReturnsTodosFromEveryUser()
    {
        // Arrange
        var userTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        var operatorTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoOperator).Build());
        var adminTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/filter", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTodosAsync(response);
        Assert.Equal(3, body.Count);
        Assert.Contains(body, todo => todo.Id == userTodo.Id);
        Assert.Contains(body, todo => todo.Id == operatorTodo.Id);
        Assert.Contains(body, todo => todo.Id == adminTodo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithCreatedBy_ReturnsOnlyThatUsersTodos()
    {
        // Arrange
        var userTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoOperator).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { createdBy = new[] { TestUsers.Email(TestUser.TodoUser) } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = Assert.Single(await ReadTodosAsync(response));
        Assert.Equal(userTodo.Id, todo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_AsAdmin_WithSeveralCreatedBy_ReturnsTheUnionOfTheirTodos()
    {
        // Arrange
        var userTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).Build());
        var operatorTodo = await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoOperator).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoAdmin).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new
            {
                createdBy = new[]
                {
                    TestUsers.Email(TestUser.TodoUser),
                    TestUsers.Email(TestUser.TodoOperator),
                },
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTodosAsync(response);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, todo => todo.Id == userTodo.Id);
        Assert.Contains(body, todo => todo.Id == operatorTodo.Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithSortAndPagination_ReturnsOnlyTheRequestedPage()
    {
        // Arrange
        var first = TestData.Unique("aaa-titles");
        var second = TestData.Unique("bbb-titles");
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(first).Build());
        await Fixture.Todos.AddAsync(TodoOwnedBy(TestUser.TodoUser).WithTitle(second).Build());

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/titles",
            new
            {
                sortProperties = new[] { new { key = "Title", value = false } },
                pageNumber = 2,
                pageSize = 1,
            },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TodoTitleResponse>>(
            JsonSerializerOptions.Web, CancellationToken);
        var title = Assert.Single(body!);
        Assert.Equal(second, title.Title);
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
            "/api/Todo/filter", new { startDate, endDate }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithUnknownSortProperty_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
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
            "/api/Todo/filter", new { pageNumber = 1 }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithPageSizeWithoutPageNumber_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/titles", new { pageSize = 1 }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static TodoBuilder TodoOwnedBy(TestUser user) =>
        new TodoBuilder().CreatedBy(TestUsers.Email(user));

    private static async Task<List<TodoResponse>> ReadTodosAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<TodoResponse>>(JsonSerializerOptions.Web, TestContext.Current.CancellationToken) ?? [];
}
