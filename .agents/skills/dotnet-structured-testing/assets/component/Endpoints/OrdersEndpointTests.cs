using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{AppNamespace}};

namespace {{ComponentTestsNamespace}}.Endpoints;

// EXAMPLE — one test class per endpoint group, in a folder that mirrors the production code.
// Coverage for an endpoint: success (response + database + messages), validation, not found/conflict,
// 401 anonymous, 403 per forbidden role, allowed roles, third-party failure.
public class OrdersEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.Customer)]
    public async Task CreateOrder_WithValidCustomer_PersistsOrderAndPublishesEvent()
    {
        // Arrange
        var customer = await Fixture.Customers.AddAsync(new CustomerBuilder().Build());
        var request = new CreateOrderRequest(customer.Id, Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await Fixture.Orders.GetSingleByCustomerAsync(customer.Id);
        Assert.Equal(100m, order.Amount);
        var published = await Fixture.Kafka.WaitForMessageAsync<OrderCreated>("orders", m => m.OrderId == order.Id);
        Assert.NotNull(published);
    }

    [Theory]
    [ResetDatabase]
    [InlineData(TestUser.Admin, HttpStatusCode.NoContent)]
    [InlineData(TestUser.Customer, HttpStatusCode.Forbidden)]
    [InlineData(TestUser.AuthenticatedWithoutRoles, HttpStatusCode.Forbidden)]
    public async Task DeleteOrder_ByUserRole_ReturnsExpectedStatus(TestUser user, HttpStatusCode expected)
    {
        // Arrange
        var order = await Fixture.Orders.AddAsync(new OrderBuilder().Build());
        var client = CreateClient(user);

        // Act
        var response = await client.DeleteAsync($"/orders/{order.Id}", CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateOrderRequest(Guid.NewGuid(), Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [AuthenticatedAs(TestUser.Customer)]
    public async Task CreateOrder_WithNonPositiveAmount_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateOrderRequest(Guid.NewGuid(), Amount: 0m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains(nameof(CreateOrderRequest.Amount), problem!.Errors.Keys);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.Customer)]
    public async Task CreateOrder_WhenPaymentProviderFails_ReturnsServiceUnavailableAndPersistsNothing()
    {
        // Arrange
        var customer = await Fixture.Customers.AddAsync(new CustomerBuilder().Build());
        await Fixture.WireMock.StubJsonAsync("POST", "/payments", statusCode: 500);
        var request = new CreateOrderRequest(customer.Id, Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, await Fixture.Orders.CountAsync());
        Assert.NotEmpty(await Fixture.WireMock.GetRequestsAsync("POST", "/payments"));
    }
}
