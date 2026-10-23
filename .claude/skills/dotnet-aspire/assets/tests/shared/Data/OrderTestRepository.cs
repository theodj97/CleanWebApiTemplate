using {{AppNamespace}};
using {{SharedNamespace}}.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace {{SharedNamespace}}.Data;

// EXAMPLE — one test repository per aggregate/table, exposed by the fixture (Fixture.Orders). Apps without EF: the
// same shape over Dapper/ADO.NET with the connection string (references/database.md).
/// <summary>Arrange: AddAsync inserts and returns the entity. Assert: query methods read current state.</summary>
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
    public async Task<Order> GetSingleByCustomerAsync(string subject)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.Orders.AsNoTracking().SingleAsync(o => o.CustomerSubject == subject, TestCancellation.Token);
    }

    public async Task<int> CountAsync()
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.Orders.CountAsync(TestCancellation.Token);
    }
}
