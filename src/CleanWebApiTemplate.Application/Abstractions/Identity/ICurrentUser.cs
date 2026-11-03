namespace CleanWebApiTemplate.Application.Abstractions.Identity;

/// <summary>
/// The authenticated caller as the application sees it, without ASP.NET types.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The caller's email. Throws when the authenticated principal has no email claim.</summary>
    string Email { get; }

    /// <summary>True when the caller has the admin role.</summary>
    bool IsAdmin { get; }

    /// <summary>The caller's effective owner scope, resolved by <see cref="OwnerScope"/>.</summary>
    OwnerScope Owner { get; }
}
