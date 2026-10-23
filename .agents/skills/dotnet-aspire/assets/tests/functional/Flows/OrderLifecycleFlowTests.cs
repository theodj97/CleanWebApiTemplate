using System.Net;
using System.Net.Http.Json;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;

namespace {{FunctionalTestsNamespace}}.Flows;

// EXAMPLE — few functional tests, each a complete journey through the running system.
/// <summary>Black box: the API runs as its own process under the shared AppHost; DTOs are declared here on purpose.</summary>
public class OrderLifecycleFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    private sealed record OrderDto(Guid Id, string CustomerSubject, decimal Amount);

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task CustomerCreatesOrder_AdminSeesIt()
    {
        // Arrange
        var admin = CreateClient(TestUser.OrdersAdmin);

        // Act
        var created = await Client.PostAsJsonAsync("/orders", new { Amount = 42.5m }, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = await created.Content.ReadFromJsonAsync<OrderDto>(CancellationToken);
        var all = await admin.GetFromJsonAsync<List<OrderDto>>("/orders", CancellationToken);
        Assert.Contains(all!, o => o.Id == order!.Id && o.CustomerSubject == TestUsers.Get(TestUser.OrdersCustomer).Subject);
    }

    [Fact]
    [ResetDatabase]
    public async Task UserLogsInWithOwnEndpoint_TokenIsAccepted()
    {
        // Arrange
        var (user, password) = await Fixture.Users.AddAsync("Customer");
        var login = await Client.PostAsJsonAsync("/auth/login", new { user.UserName, Password = password }, CancellationToken);
        var token = (await login.Content.ReadFromJsonAsync<Dictionary<string, string>>(CancellationToken))!["accessToken"];
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        var response = await client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
