using {{AppNamespace}};
using {{SharedNamespace}}.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace {{SharedNamespace}}.Data;

// EXAMPLE — only for applications with their own users table and login endpoint.
/// <summary>Users for the application's own login endpoint (the endpoint itself is tested as a normal endpoint).</summary>
public sealed class UserTestRepository(IServiceScopeFactory scopes)
{
    /// <summary>Inserts a user with a unique name and returns it with the clear-text password the test logs in with.</summary>
    public async Task<(User User, string Password)> AddAsync(string role)
    {
        var password = TestData.Unique("pwd");
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = TestData.Unique("user"),
            PasswordHash = PasswordHashing.Hash(password),
            Role = role,
        };

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync(TestCancellation.Token);
        return (user, password);
    }
}
