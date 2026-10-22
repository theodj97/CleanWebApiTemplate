using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using {{AppNamespace}};

namespace {{UnitTestsNamespace}}.Orders;

// EXAMPLE — one class (or a small cohesive group) in isolation. Every dependency is an NSubstitute substitute;
// no I/O, network, database, Docker or Aspire. These tests run in parallel and finish in milliseconds.
public class OrderPricingServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 3, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly IDiscountPolicy _discounts = Substitute.For<IDiscountPolicy>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly FakeTimeProvider _time = new(Now);

    public static TheoryData<decimal, decimal, decimal> Discounts => new()
    {
        { 100m, 0m, 100m },
        { 100m, 10m, 90m },
        { 80m, 25m, 60m },
    };

    [Theory]
    [MemberData(nameof(Discounts))]
    public void CalculateTotal_WithDiscountPercentage_AppliesDiscount(decimal subtotal, decimal percentage, decimal expected)
    {
        // Arrange
        _discounts.GetPercentage(Arg.Any<Guid>(), Now).Returns(percentage);
        var sut = new OrderPricingService(_discounts, _orders, _time);

        // Act
        var total = sut.CalculateTotal(Guid.NewGuid(), subtotal);

        // Assert
        Assert.Equal(expected, total);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderDoesNotExist_ThrowsOrderNotFound()
    {
        // Arrange
        _orders.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var sut = new OrderPricingService(_discounts, _orders, _time);

        // Act
        var act = () => sut.ConfirmAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<OrderNotFoundException>(act);
    }

    [Fact]
    public async Task ConfirmAsync_WithPendingOrder_SavesConfirmationTimestamp()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), 100m);
        _orders.FindAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var sut = new OrderPricingService(_discounts, _orders, _time);

        // Act
        await sut.ConfirmAsync(order.Id, TestContext.Current.CancellationToken);

        // Assert
        await _orders.Received(1).SaveAsync(
            Arg.Is<Order>(o => o.Id == order.Id && o.ConfirmedAt == Now),
            Arg.Any<CancellationToken>());
    }
}
