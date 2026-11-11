using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanWebApiTemplate.Domain.Project;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.FunctionalTests.Project;

/// <summary>One complete Project journey through real HTTP: create, read, update and delete.</summary>
public class ProjectLifecycleFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task ProjectLifecycle_CreateReadUpdateDelete_Succeeds()
    {
        // Arrange
        var name = TestData.Unique("flow");

        // Act - create
        var create = await Client.PostAsJsonAsync(
            "/api/projects/", new { name, description = "functional flow" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ProjectResponseDto>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(created);

        // Act - read
        var read = await Client.GetAsync($"/api/projects/{created!.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var fetched = await read.Content.ReadFromJsonAsync<ProjectResponseDto>(JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(fetched);
        Assert.Equal(name, fetched!.Name);

        // Act - update
        var update = await Client.PutAsJsonAsync(
            $"/api/projects/{created.Id}", new { status = (int)EProjectStatus.Completed }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var stored = await Fixture.Projects.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(EProjectStatus.Completed, stored!.Status);

        // Act - delete
        var delete = await Client.DeleteAsync($"/api/projects/{created.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Assert
        Assert.Null(await Fixture.Projects.FindAsync(created.Id));
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }
}
