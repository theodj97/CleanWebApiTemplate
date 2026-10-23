using {{SharedNamespace}}.Auth;

namespace {{SharedNamespace}}.Attributes;

/// <summary>
/// The test's <c>Client</c> sends a token for this user. On a class it applies to every test; a method attribute
/// (<c>[AuthenticatedAs]</c> or <c>[Unauthenticated]</c>) takes precedence. Without any attribute the client is
/// anonymous, so who makes each call is always visible in the test itself.
/// When the identity varies per <c>[Theory]</c> row, or a test needs several identities, use
/// <c>CreateClient(TestUser)</c> in the Arrange section instead: attribute values cannot change per InlineData row.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthenticatedAsAttribute(TestUser user) : Attribute
{
    public TestUser User { get; } = user;
}
