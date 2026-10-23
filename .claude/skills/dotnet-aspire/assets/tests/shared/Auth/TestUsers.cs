namespace {{SharedNamespace}}.Auth;

/// <summary>Claims of one test identity. Everything is fixed so that tokens and data linked to a user are deterministic.</summary>
public sealed record TestUserDefinition(
    string Subject,
    string Name,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, object> AdditionalClaims);

/// <summary>
/// ADAPT: fixed definitions of every <see cref="TestUser"/>. Roles and scopes are the exact values the applications check;
/// the claim TYPES they are emitted with depend on the target application (<see cref="TestAuthSettings.For"/>).
/// If an application requires users to exist in its database, TestServerFixtureBase.SeedPersistentDataAsync inserts them.
/// </summary>
public static class TestUsers
{
    private static readonly IReadOnlyDictionary<string, object> NoClaims = new Dictionary<string, object>();

    private static readonly Dictionary<TestUser, TestUserDefinition> Definitions = new()
    {
        [TestUser.OrdersAdmin] = new("7d9f3c1e-0000-4000-8000-000000000001", "Test Orders Admin", ["Admin"], [], NoClaims),
        [TestUser.OrdersCustomer] = new("7d9f3c1e-0000-4000-8000-000000000002", "Test Customer", ["Customer"], [], NoClaims),
        [TestUser.CatalogReader] = new("7d9f3c1e-0000-4000-8000-000000000003", "Test Catalog Reader", [], ["catalog.read"], NoClaims),
        [TestUser.CatalogEditor] = new("7d9f3c1e-0000-4000-8000-000000000004", "Test Catalog Editor", ["Catalog.Editor"], ["catalog.read", "catalog.write"], NoClaims),
        [TestUser.CatalogEditorWithoutWriteScope] = new("7d9f3c1e-0000-4000-8000-000000000005", "Test Editor No Scope", ["Catalog.Editor"], ["catalog.read"], NoClaims),
        [TestUser.AuthenticatedWithoutRoles] = new("7d9f3c1e-0000-4000-8000-000000000006", "Test NoRoles", [], [], NoClaims),
    };

    public static TestUserDefinition Get(TestUser user) =>
        Definitions.TryGetValue(user, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(user), user, "Add a definition for this TestUser in TestUsers.");

    public static IEnumerable<KeyValuePair<TestUser, TestUserDefinition>> All => Definitions;
}
