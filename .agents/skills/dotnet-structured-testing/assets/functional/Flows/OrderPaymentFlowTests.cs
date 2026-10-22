using System.Net;
using System.Net.Http.Json;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{SharedNamespace}}.Waiting;
using {{AppNamespace}};

namespace {{FunctionalTestsNamespace}}.Flows;

// EXAMPLE — few functional tests, each one a complete business journey through the running system.
public class OrderPaymentFlowTests(FunctionalTestFixture fixture) : FunctionalTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.Customer)]
    public async Task PayOrder_WhenProviderApproves_PublishesPaymentCompletedAndMarksOrderPaid()
    {
        // Arrange
        var customer = await Fixture.Customers.AddAsync(new CustomerBuilder().Build());
        var order = await Fixture.Orders.AddAsync(new OrderBuilder().ForCustomer(customer.Id).WithAmount(100m).Build());
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
    }
}
