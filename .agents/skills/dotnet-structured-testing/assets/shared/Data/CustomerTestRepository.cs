using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using {{SharedNamespace}}.Fixtures;
using {{AppNamespace}};

namespace {{SharedNamespace}}.Data;

// EXAMPLE — see OrderTestRepository.
public sealed class CustomerTestRepository(IServiceScopeFactory scopes)
{
    public async Task<Customer> AddAsync(Customer customer)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        db.Customers.Add(customer);
        await db.SaveChangesAsync(TestCancellation.Token);

        return customer;
    }

    public async Task<Customer?> FindAsync(Guid id)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        return await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, TestCancellation.Token);
    }
}
