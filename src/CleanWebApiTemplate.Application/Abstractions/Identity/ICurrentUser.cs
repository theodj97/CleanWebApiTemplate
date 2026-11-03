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

    /// <summary>
    /// The owner filter for repository queries: null for admins (they see every owner),
    /// the caller's email for everyone else.
    /// </summary>
    string? OwnerScope { get; }
}
