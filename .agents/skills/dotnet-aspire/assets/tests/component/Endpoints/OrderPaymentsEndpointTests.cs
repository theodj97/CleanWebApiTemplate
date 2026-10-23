using System.Net;
using System.Net.Http.Json;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{AppNamespace}};

namespace {{ComponentTestsNamespace}}.Endpoints;

// EXAMPLE — an endpoint with side effects beyond the database: it publishes an event and calls a third-party API.
// Needs the Kafka and WireMock members of TestServerFixtureBase and the matching AppHost resources (ADAPT blocks).
// Coverage: success (response + database + published message + third-party call) and third-party failure
// (documented status, nothing persisted, the call was attempted).
public class OrderPaymentsEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    private static readonly string CustomerSubject = TestUsers.Get(TestUser.OrdersCustomer).Subject;

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task CreateOrder_WithValidCustomer_PersistsOrderAndPublishesEvent()
    {
        // Arrange
        await Fixture.WireMock.StubJsonAsync("POST", "/payments", statusCode: 200, body: new { status = "approved" });
        var request = new CreateOrderRequest(Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await Fixture.Orders.GetSingleByCustomerAsync(CustomerSubject);
        Assert.Equal(100m, order.Amount);
        var published = await Fixture.Kafka.WaitForMessageAsync<OrderCreated>("orders", m => m.OrderId == order.Id);
        Assert.NotNull(published);
        Assert.Single(await Fixture.WireMock.GetRequestsAsync("POST", "/payments"));
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task CreateOrder_WhenPaymentProviderFails_ReturnsServiceUnavailableAndPersistsNothing()
    {
        // Arrange
        await Fixture.WireMock.StubJsonAsync("POST", "/payments", statusCode: 500);
        var request = new CreateOrderRequest(Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, await Fixture.Orders.CountAsync());
        // Resilience handlers retry: assert the calls the policy implies, or NotEmpty, never Single.
        Assert.NotEmpty(await Fixture.WireMock.GetRequestsAsync("POST", "/payments"));
    }
}
