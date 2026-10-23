using System.Net;
using System.Net.Http.Json;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{SharedNamespace}}.Waiting;
using {{AppNamespace}};

namespace {{FunctionalTestsNamespace}}.Flows;

// EXAMPLE — a flow that calls a third-party API and publishes to Kafka. Needs the Kafka and WireMock members of
// TestServerFixtureBase and the matching resources in the AppHost (both shown as ADAPT blocks in the templates).
public class OrderPaymentFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task PayOrder_WhenProviderApproves_PublishesPaymentCompletedAndMarksOrderPaid()
    {
        // Arrange
        var customer = TestUsers.Get(TestUser.OrdersCustomer).Subject;
        var order = await Fixture.Orders.AddAsync(new OrderBuilder().ForCustomer(customer).WithAmount(100m).Build());
        await Fixture.WireMock.StubJsonAsync("POST", "/payments", statusCode: 200, body: new { status = "approved" });

        // Act
        var response = await Client.PostAsync($"/orders/{order.Id}/payment", content: null, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var published = await Fixture.Kafka.WaitForMessageAsync<PaymentCompleted>("payments", m => m.OrderId == order.Id);
        Assert.NotNull(published);
        await Eventually.AssertAsync(async () =>
        {
            var current = await Client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", CancellationToken);
            Assert.Equal("Paid", current!.Status);
        });
        Assert.Single(await Fixture.WireMock.GetRequestsAsync("POST", "/payments"));
        Assert.Empty(Fixture.OutboundCalls.Requests);
    }
}
