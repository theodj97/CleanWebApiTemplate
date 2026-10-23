namespace {{SharedNamespace}}.Auth;

/// <summary>
/// ADAPT: identities available to tests, generated from the roles, scopes and policies found in discovery:
/// <list type="bullet">
/// <item>one user per role (per application when the applications use different role sets);</item>
/// <item>one user per role/claim combination that a policy requires (catalog.write needs the role AND the scope);</item>
/// <item>one authenticated user without roles or scopes, to prove that protected endpoints answer 403.</item>
/// </list>
/// An enum, not strings: the compiler checks that the user exists, and enums can be used in attributes and InlineData.
/// </summary>
public enum TestUser
{
    OrdersAdmin,
    OrdersCustomer,
    CatalogReader,
    CatalogEditor,
    CatalogEditorWithoutWriteScope,
    AuthenticatedWithoutRoles,
}
