namespace {{SharedNamespace}}.Auth;

/// <summary>
/// Identities available to tests.
/// ADAPT: generate the values from the roles and policies found in discovery:
/// <list type="bullet">
/// <item>one user per role;</item>
/// <item>one user per role/claim combination that a policy requires (e.g. Support + "tenant" claim);</item>
/// <item>one authenticated user without roles, to prove that protected endpoints answer 403.</item>
/// </list>
/// An enum, not strings: the compiler checks that the user exists, and enums can be used in attributes and InlineData.
/// </summary>
public enum TestUser
{
    Admin,
    Customer,
    Support,
    AuthenticatedWithoutRoles,
}
