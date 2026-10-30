using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// Validation branches at the HTTP edge: malformed ids, paging, dates, statuses, sort keys and
/// titles must become 400 ProblemDetails before they reach the repository.
/// </summary>
public class TodoValidationTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task GetById_WithInvalidId_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/Todo/not-a-ulid", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithInvalidId_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/Todo/not-a-ulid", new { description = "ignored" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task Delete_WithInvalidId_ReturnsBadRequest()
    {
        var response = await Client.DeleteAsync("/api/Todo/not-a-ulid", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoOperator)]
    [ResetDatabase]
    public async Task Delete_WithUnknownId_ReturnsOkAndFalse()
    {
        var response = await Client.DeleteAsync($"/api/Todo/{Ulid.NewUlid()}", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await response.Content.ReadFromJsonAsync<bool>(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithInvalidSortKey_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { sortProperties = new[] { new { key = "Password", value = false } } },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithInvalidSortKey_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/titles",
            new { sortProperties = new[] { new { key = "Password", value = false } } },
            CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithIncompleteOrZeroPaging_ReturnsBadRequest(int? pageNumber, int? pageSize)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { pageNumber, pageSize }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(null, 1)]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithIncompletePaging_ReturnsBadRequest(int? pageNumber, int? pageSize)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/titles", new { pageNumber, pageSize }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2026-01-01T00:00:00Z", null)]
    [InlineData(null, "2026-01-01T00:00:00Z")]
    [InlineData("2026-02-01T00:00:00Z", "2026-01-01T00:00:00Z")]
    [InlineData("not-a-date", "2026-01-01T00:00:00Z")]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithInvalidDateRange_ReturnsBadRequest(string? startDate, string? endDate)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { startDate, endDate }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithUnknownStatus_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { status = new[] { 99 } }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithInvalidId_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { ids = new[] { "not-a-ulid" } }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoAdmin)]
    public async Task Filter_WithInvalidOwnerEmail_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { createdBy = new[] { "not-an-email" } }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithOverlongTitleFragment_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter", new { title = new[] { new string('a', 256) } }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [AuthenticatedAs(TestUser.TodoUser)]
    [ResetDatabase]
    public async Task Update_WithEmptyOrWhitespaceTitle_ReturnsBadRequest(string title)
    {
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{existing.Id}", new { title }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithNullBody_ReturnsBadRequest()
    {
        using var content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/Todo/", content, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
