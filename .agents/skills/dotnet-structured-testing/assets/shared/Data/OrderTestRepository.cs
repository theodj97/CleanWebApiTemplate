using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using {{SharedNamespace}}.Fixtures;
using {{AppNamespace}};

namespace {{SharedNamespace}}.Data;

// EXAMPLE — one test repository per aggregate/table, exposed by the fixture (Fixture.Orders).
// Arrange: AddAsync inserts and returns the entity with its generated id. Assert: query methods read current state.
// Each call opens a fresh DbContext, so a query never returns stale tracked entities.
// Apps without EF: same shape over Dapper/plain SQL with the connection string (see references/database.md).
public sealed class OrderTestRepository(IServiceScopeFactory scopes)
{
    public async Task<Order> AddAsync(Order order)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        db.Orders.Add(order);
        await db.SaveChangesAsync(TestCancellation.Token);

        return order;
    }

    public async Task<Order?> FindAsync(Guid id)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        return await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id, TestCancellation.Token);
    }

    /// <summary>Fails if there is not exactly one order for the customer: that is part of what the test verifies.</summary>
    public async Task<Order> GetSingleByCustomerAsync(Guid customerId)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        return await db.Orders.AsNoTracking().SingleAsync(o => o.CustomerId == customerId, TestCancellation.Token);
    }

    public async Task<int> CountAsync()
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        return await db.Orders.CountAsync(TestCancellation.Token);
    }
}
