namespace CleanWebApiTemplate.Application.Common.Identity;

/// <summary>
/// The caller's effective ownership. Admins may see and act on every owner; everyone else is
/// pinned to their own email. The rule lives here so HTTP mappers and handlers do not re-derive it.
/// </summary>
public readonly record struct OwnerScope(string Email, bool IsAdmin)
{
    /// <summary>Owner filter for a single-item read, update or delete: null for admins, the caller email otherwise.</summary>
    public string? VisibleOwner => IsAdmin ? null : Email;

    /// <summary>Owner of an item this caller creates: always the caller, even for admins.</summary>
    public string OwnerForCreate => Email;

    /// <summary>
    /// Owner filter for a list request: admins keep the requested owners; everyone else is pinned to
    /// their own email.
    /// </summary>
    public IReadOnlyList<string>? FilterOwners(IReadOnlyList<string>? requested) =>
        IsAdmin ? requested : [Email];
}
