using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using {App}.Tests.Shared.Attributes;
using {App}.Tests.Shared.Auth;
using {App}.Tests.Shared.Data;

namespace {App}.Api.FunctionalTests.Project;

/// <summary>One complete Project journey through real HTTP: create, read and filter.</summary>
public class ProjectLifecycleFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.TodoOperator)]
    public async Task ProjectLifecycle_CreateReadFilter_Succeeds()
    {
        // Arrange
        string name = TestData.Unique("flow");

        // Act - create
        HttpResponseMessage create = await Client.PostAsJsonAsync(
            "/api/Project/", new { name, description = "functional flow" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        ProjectResponseDto? created = await create.Content.ReadFromJsonAsync<ProjectResponseDto>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(created);

        // Act - read
        HttpResponseMessage read = await Client.GetAsync($"/api/Project/{created!.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        ProjectResponseDto? fetched = await read.Content.ReadFromJsonAsync<ProjectResponseDto>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(fetched);
        Assert.Equal(name, fetched!.Name);

        // Act - filter
        HttpResponseMessage filter = await Client.PostAsJsonAsync(
            "/api/Project/filter", new { names = new[] { name } }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, filter.StatusCode);
        IEnumerable<ProjectResponseDto>? results = await filter.Content.ReadFromJsonAsync<IEnumerable<ProjectResponseDto>>(
            JsonSerializerOptions.Web, CancellationToken);
        Assert.NotNull(results);
        Assert.Single(results!);

        // Assert: the flow never called a real external service.
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }
}
