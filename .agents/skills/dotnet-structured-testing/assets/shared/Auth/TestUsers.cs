namespace {{SharedNamespace}}.Auth;

/// <summary>Claims of one test identity. Everything is fixed so that tokens and data linked to a user are deterministic.</summary>
public sealed record TestUserDefinition(
    string Subject,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, object> AdditionalClaims);

/// <summary>
/// Fixed definitions of every <see cref="TestUser"/>. The same user always has the same subject ("sub"/id), so rows
/// keyed by the user (owner columns, user tables linked by "sub", audit fields) are predictable across runs.
/// ADAPT: roles with the exact values the app checks; additional claims the app depends on (scopes, tenant, oid...).
/// If the app requires users to exist in its database, TestServerFixtureBase.SeedPersistentDataAsync inserts these.
/// </summary>
public static class TestUsers
{
    private static readonly IReadOnlyDictionary<string, object> NoClaims = new Dictionary<string, object>();

    // Example of an extra claim set. Microsoft.Identity.Web APIs reject tokens with neither "scp" nor "roles".
    private static readonly IReadOnlyDictionary<string, object> ApiScopes = new Dictionary<string, object>
    {
        ["scp"] = "orders.read orders.write",
    };

    private static readonly Dictionary<TestUser, TestUserDefinition> Definitions = new()
    {
        [TestUser.Admin] = new("7d9f3c1e-0000-4000-8000-000000000001", "Test Admin", "admin@example.com", ["Admin"], ApiScopes),
        [TestUser.Customer] = new("7d9f3c1e-0000-4000-8000-000000000002", "Test Customer", "customer@example.com", ["Customer"], ApiScopes),
        [TestUser.Support] = new("7d9f3c1e-0000-4000-8000-000000000003", "Test Support", "support@example.com", ["Support"], ApiScopes),
        [TestUser.AuthenticatedWithoutRoles] = new("7d9f3c1e-0000-4000-8000-000000000004", "Test NoRoles", "noroles@example.com", [], ApiScopes),
    };

    public static TestUserDefinition Get(TestUser user) =>
        Definitions.TryGetValue(user, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(user), user, "Add a definition for this TestUser in TestUsers.");

    public static IEnumerable<KeyValuePair<TestUser, TestUserDefinition>> All => Definitions;
}
