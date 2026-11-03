namespace CleanWebApiTemplate.Application.Abstractions.Identity;

/// <summary>
/// The caller's effective ownership. Admins may see and act on every owner; everyone else is
/// pinned to their own email. The rule lives here so HTTP mappers and handlers do not re-derive it.
/// </summary>
public readonly record struct OwnerScope(string Email, bool IsAdmin)
{
    /// <summary>Owner filter for reads: null for admins, the caller email otherwise.</summary>
    public string? VisibleOwner => IsAdmin ? null : Email;

    /// <summary>Owner for a new Todo: always the caller, even for admins.</summary>
    public string OwnerForCreate => Email;

    /// <summary>
    /// Owner filter for a filter request: admins keep the requested owners; everyone else is
    /// pinned to their own email.
    /// </summary>
    public IReadOnlyList<string>? FilterOwners(IReadOnlyList<string>? requested) =>
        IsAdmin ? requested : new[] { Email };
}
