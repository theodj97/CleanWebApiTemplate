using CleanWebApiTemplate.Tests.Shared.Auth;

namespace CleanWebApiTemplate.Tests.Shared.Attributes;

/// <summary>
/// The test's <c>Client</c> sends a token for this user. On a class it applies to every test; a method
/// attribute (<see cref="AuthenticatedAsAttribute"/> or <see cref="UnauthenticatedAttribute"/>) takes precedence.
/// Without any attribute the client is anonymous, so who makes each call is always visible in the test itself.
/// When the identity varies per <c>[Theory]</c> row, use <c>CreateClient(TestUser)</c> in Arrange instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthenticatedAsAttribute(TestUser user) : Attribute
{
    public TestUser User { get; } = user;
}
