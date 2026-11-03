namespace CleanWebApiTemplate.Tests.Shared.Auth;

/// <summary>Claims of one test identity. Everything is fixed so tokens and linked data are deterministic.</summary>
public sealed record TestUserDefinition(
    string Subject,
    string Name,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, object> AdditionalClaims);

/// <summary>
/// Fixed definitions of every <see cref="TestUser"/>. Roles are the exact values the policies check
/// (<c>RequireRole("User")</c>, ...). The claim TYPES they are emitted with depend on the target
/// application (<see cref="TestAuthSettings.For"/>).
/// </summary>
public static class TestUsers
{
    private static readonly Dictionary<TestUser, TestUserDefinition> Definitions = new()
    {
        [TestUser.TodoUser] = new(
            "7d9f3c1e-0000-4000-8000-000000000001", "Test Todo User", ["User"], [],
            Claims("test.todo.user@example.com")),
        [TestUser.TodoOperator] = new(
            "7d9f3c1e-0000-4000-8000-000000000002", "Test Todo Operator", ["Operator"], [],
            Claims("test.todo.operator@example.com")),
        [TestUser.TodoAdmin] = new(
            "7d9f3c1e-0000-4000-8000-000000000003", "Test Todo Admin", ["Admin"], [],
            Claims("test.todo.admin@example.com")),
        [TestUser.TodoExternal] = new(
            "7d9f3c1e-0000-4000-8000-000000000004", "Test Todo External", ["External"], [],
            Claims("test.todo.external@example.com")),
        [TestUser.AuthenticatedWithoutRoles] = new(
            "7d9f3c1e-0000-4000-8000-000000000005", "Test No Roles", [], [],
            Claims("test.todo.norole@example.com")),
        [TestUser.AuthenticatedWithoutEmail] = new(
            "7d9f3c1e-0000-4000-8000-000000000006", "Test No Email", ["User"], [],
            new Dictionary<string, object>()),
    };

    public static TestUserDefinition Get(TestUser user) =>
        Definitions.TryGetValue(user, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(user), user, "Add a definition for this TestUser in TestUsers.");

    /// <summary>The email carried by the token; the API stores it as CreatedBy/UpdatedBy.</summary>
    public static string Email(TestUser user) => (string)Get(user).AdditionalClaims["email"];

    public static IEnumerable<KeyValuePair<TestUser, TestUserDefinition>> All => Definitions;

    private static IReadOnlyDictionary<string, object> Claims(string email) =>
        new Dictionary<string, object> { ["email"] = email };
}
