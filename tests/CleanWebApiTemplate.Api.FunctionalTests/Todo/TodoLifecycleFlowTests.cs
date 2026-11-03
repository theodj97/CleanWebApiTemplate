using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.FunctionalTests.Todo;

/// <summary>One complete Todo journey through real HTTP: create, read, update and delete.</summary>
public class TodoLifecycleFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task TodoLifecycle_CreateReadUpdateDelete_Succeeds()
    {
        // Arrange
        var title = TestData.Unique("flow");

        // Act - create
        var create = await Client.PostAsJsonAsync(
            "/api/Todo/", new { title, description = "functional flow" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<TodoResponseDto>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(created);

        // Act - read
        var read = await Client.GetAsync($"/api/Todo/{created!.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var fetched = await read.Content.ReadFromJsonAsync<TodoResponseDto>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(fetched);
        Assert.Equal(title, fetched!.Title);

        // Act - update
        var update = await Client.PutAsJsonAsync(
            $"/api/Todo/{created.Id}", new { status = (int)ETodoStatus.Completed }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var stored = await Fixture.Todos.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal((int)ETodoStatus.Completed, stored!.Status);

        // Act - delete
        var delete = await Client.DeleteAsync($"/api/Todo/{created.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        // Assert
        Assert.Null(await Fixture.Todos.FindAsync(created.Id));
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }
}
