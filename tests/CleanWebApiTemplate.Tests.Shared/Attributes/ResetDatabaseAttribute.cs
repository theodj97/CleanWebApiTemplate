namespace CleanWebApiTemplate.Tests.Shared.Attributes;

/// <summary>
/// Declares that the test (or every test of the class) must start from empty databases.
/// <c>[ResetDatabase]</c> resets every registered database; <c>[ResetDatabase("TodoDb")]</c> only those.
/// The reset runs before the test (never after), so every test starts from a known state even if the
/// previous one was interrupted, and a failed test leaves its data for inspection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResetDatabaseAttribute : Attribute
{
    public ResetDatabaseAttribute(params string[] databases) => Databases = databases;

    /// <summary>Logical database names. Empty means every registered database.</summary>
    public IReadOnlyList<string> Databases { get; }

    /// <summary>Additional data to keep for this test only: a table name ("Todo" or "public.Todo").</summary>
    public string[] ExcludeTables { get; set; } = [];
}
