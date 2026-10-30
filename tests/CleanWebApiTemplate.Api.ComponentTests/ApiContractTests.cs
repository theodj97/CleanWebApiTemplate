using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests;

/// <summary>
/// HTTP contract at the edges: client errors are 400s, unmatched routes/methods are ProblemDetails
/// and empty collections are 204.
/// </summary>
public class ApiContractTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    public async Task Get_UnknownRoute_ReturnsProblemDetails()
    {
        // Act
        var response = await Client.GetAsync("/nope", CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Not Found");
    }

    [Fact]
    public async Task Get_UnsupportedMethod_ReturnsProblemDetails()
    {
        // Act
        var response = await Client.GetAsync("/api/Todo/", CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/Todo/", content, CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Bad Request");
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithMissingTitle_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/", new { }, CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Bad Request");
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Create_WithNullTitle_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title = (string?)null }, CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Validation Errors");
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Update_WithStatusZero_ReturnsBadRequestAndKeepsTheStoredStatus()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(
            new TodoBuilder().CreatedBy(TestUsers.Email(TestUser.TodoUser)).Build());

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/Todo/{existing.Id}", new { status = 0 }, CancellationToken);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest, "Validation Errors");
        var stored = await Fixture.Todos.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal((int)ETodoStatus.Pending, stored!.Status);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Filter_WithNoMatches_ReturnsNoContent()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/Todo/filter",
            new { ids = new[] { Ulid.NewUlid().ToString() } },
            CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoUser)]
    public async Task Titles_WithNoRows_ReturnsNoContent()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/Todo/titles", new { }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response,
                                                       HttpStatusCode expectedStatus,
                                                       string expectedTitle)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = document.RootElement;

        Assert.Equal((int)expectedStatus, root.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, root.GetProperty("title").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }
}
