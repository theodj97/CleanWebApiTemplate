# Owner identified by the email claim

An item's owner is the `email` claim of the JWT, exposed through `ICurrentUser` and `OwnerScope`. The column is
`CreatedBy`; visibility, creation and filtering all derive from the claim. The alternative — a stable subject
(`sub`/`oid`) — was rejected for readability: the seed, the devtoken profiles, the Swagger users and the tests
all speak email, and a reviewer can see who owns a row without joining an identity table.

## Consequences

- Changing a user's email in the identity provider orphans their rows: ownership does not follow the new
  address.
- A provider that does not emit `email` (for example Entra ID without the optional claim) makes every request
  fail with 403 until the claim is configured.
- The mapping lives in one place (`HttpContextCurrentUser`); switching to `sub` means changing it and
  migrating the columns.
- Admin is the only role that sees every owner; there is no sharing or transfer.
