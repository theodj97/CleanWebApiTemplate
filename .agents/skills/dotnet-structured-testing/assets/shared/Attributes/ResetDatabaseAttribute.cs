namespace {{SharedNamespace}}.Attributes;

/// <summary>
/// Declares that the test (or every test of the class) must start from empty databases.
/// <list type="bullet">
/// <item><c>[ResetDatabase]</c> resets every registered database.</item>
/// <item><c>[ResetDatabase("OrdersDb", "AuditDb")]</c> resets only those (logical names, see DatabaseResetConfiguration).</item>
/// <item><c>ExcludeTables = ["Countries"]</c> keeps extra tables for that test only (collection names for MongoDB,
/// key prefixes for Redis).</item>
/// </list>
/// A method attribute takes precedence over the class attribute.
/// </summary>
/// <remarks>
/// This attribute only declares intent; <c>TestBase.InitializeAsync</c> performs the reset. xUnit attribute hooks
/// (<c>BeforeAfterTestAttribute.Before</c>) are synchronous while Respawn is asynchronous, and blocking on async I/O
/// inside xUnit is a known source of deadlocks. The reset runs before the test, never after: every test starts from a
/// known state even if the previous one was interrupted, and a failed test leaves its data behind for inspection.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResetDatabaseAttribute : Attribute
{
    public ResetDatabaseAttribute(params string[] databases) => Databases = databases;

    /// <summary>Logical database names. Empty means every registered database.</summary>
    public IReadOnlyList<string> Databases { get; }

    /// <summary>Additional data to keep for this test only: "Table" or "schema.Table", a MongoDB collection name,
    /// or a Redis key prefix, depending on the engine.</summary>
    public string[] ExcludeTables { get; set; } = [];
}
