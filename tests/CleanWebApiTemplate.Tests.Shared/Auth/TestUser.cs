namespace CleanWebApiTemplate.Tests.Shared.Auth;

/// <summary>
/// Identities available to tests. Derived from the real authorization policies
/// (<see cref="CleanWebApiTemplate.Domain.Configuration.Constants"/>): one user per role, plus an
/// authenticated user without roles to prove that protected endpoints answer 403.
/// </summary>
public enum TestUser
{
    /// <summary>Role <c>User</c>: allowed on the User policy (read, create, update).</summary>
    TodoUser,

    /// <summary>Role <c>Operator</c>: allowed on the User policy and on the Operator policy (delete).</summary>
    TodoOperator,

    /// <summary>Role <c>Admin</c>: included by every policy.</summary>
    TodoAdmin,

    /// <summary>Role <c>External</c>: only the External policy uses it (no Todo endpoint today).</summary>
    TodoExternal,

    /// <summary>Authenticated, no role: must receive 403 on every protected endpoint.</summary>
    AuthenticatedWithoutRoles,

    /// <summary>Role <c>User</c> but no email claim: must receive 403 before the handler runs.</summary>
    AuthenticatedWithoutEmail,
}
