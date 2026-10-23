using System.Net;
using System.Net.Http.Json;
using {{AppNamespace}};
using {{SharedNamespace}}.Attributes;
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using Microsoft.AspNetCore.Mvc;

namespace {{ComponentTestsNamespace}}.Endpoints;

// EXAMPLE — one test class per endpoint group, folders mirroring production.
public class OrdersEndpointTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    private static readonly string CustomerSubject = TestUsers.Get(TestUser.OrdersCustomer).Subject;

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task GetOrders_AsCustomer_ReturnsOnlyOwnOrders()
    {
        // Arrange
        var own = await Fixture.Orders.AddAsync(new OrderBuilder().ForCustomer(CustomerSubject).Build());
        await Fixture.Orders.AddAsync(new OrderBuilder().Build());

        // Act
        var response = await Client.GetAsync("/orders", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>(CancellationToken);
        Assert.Equal(own.Id, Assert.Single(orders!).Id);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task CreateOrder_AsCustomer_PersistsOrderForTokenSubject()
    {
        // Arrange
        var request = new CreateOrderRequest(Amount: 100m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await Fixture.Orders.GetSingleByCustomerAsync(CustomerSubject);
        Assert.Equal(100m, order.Amount);
    }

    [Fact]
    [ResetDatabase]
    [AuthenticatedAs(TestUser.OrdersCustomer)]
    public async Task CreateOrder_WithNonPositiveAmount_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateOrderRequest(Amount: 0m);

        // Act
        var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains("Amount", problem!.Errors.Keys);
        Assert.Equal(0, await Fixture.Orders.CountAsync());
    }

    [Theory]
    [ResetDatabase]
    [InlineData(TestUser.OrdersAdmin, HttpStatusCode.NoContent)]
    [InlineData(TestUser.OrdersCustomer, HttpStatusCode.Forbidden)]
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
}
