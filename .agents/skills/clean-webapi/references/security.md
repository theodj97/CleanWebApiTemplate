# Security

## Two authentication modes

`AuthenticationSettings.Load` selects the mode from `Authentication:Mode`, or infers it from
`Authentication:Authority` when the mode is empty (an authority means OIDC; no authority means the local
symmetric key).

| Mode | Signing | Authority | Key | Use |
|---|---|---|---|---|
| `Symmetric` | HS256 | Must be empty | `API_KEY` from OpenBao, ≥ 32 UTF-8 bytes | Local development and tests |
| `Oidc` | Provider keys | Absolute HTTPS URI | Ignored | Deployed environments |

`ConfigureServices.ConfigureAuth`:

- sets `RequireHttpsMetadata = true`;
- sets `Audience`, `MapInboundClaims = true`, and `Authority` only in OIDC mode;
- validates issuer, audience, lifetime and signing key;
- pins `ValidAlgorithms = [SecurityAlgorithms.HmacSha256]` in Symmetric mode (a token signed with another
  algorithm is rejected);
- registers the four policies before the bearer handler.

Invalid combinations fail at startup: Symmetric with an authority, OIDC without an authority or with a
non-HTTPS one, a missing/short `API_KEY` in Symmetric mode.

## Claims

`MapInboundClaims = true` maps the short JWT claims to .NET claim types:

| JWT claim | .NET claim | Read by |
|---|---|---|
| `email` | `ClaimTypes.Email` | `HttpContextCurrentUser.Email`, the email assertion in every policy |
| `role` | `ClaimTypes.Role` | `RequireRole(...)` |

The API also accepts a literal `"email"` claim in `HasEmailClaim` for providers that do not use the mapped
type. `HttpContextCurrentUser` checks `ClaimTypes.Email` first, then `"email"`.

A token without an email claim is authenticated but fails every policy → `403`. A token without a role also
gets `403`. A missing, malformed, expired, wrong-issuer, wrong-audience, wrong-key or unsigned token gets `401`.

## Policies

| Policy | Roles admitted | Typical use |
|---|---|---|
| `User` | User, Operator, Admin | Read, create, update own resources |
| `Operator` | Operator, Admin | Delete own resources, operational actions |
| `Admin` | Admin | Every resource, every owner |
| `External` | External, Operator, Admin | External integrations; no route uses it yet |

Every policy also requires an email claim. The policy names are in `Constants` (`ADMIN_POLICY`,
`OPERATOR_POLICY`, `USER_POLICY`, `EXTERNAL_POLICY`) and are used both as role values and policy names.

## Ownership

`ICurrentUser` (Application) is the only way Application sees the caller:

```csharp
public interface ICurrentUser
{
    string Email { get; }
    bool IsAdmin { get; }
    OwnerScope Owner { get; }
}
```

`OwnerScope` is a value type in `Application/Abstractions/Identity`. `HttpContextCurrentUser` (Api) reads the
claims and builds it with `new(Email, IsAdmin)`. The rule lives in the value type, not in the HTTP mappers:

- `VisibleOwner`: `null` for admins (every owner), the caller email for everyone else (read/filter/delete).
- `OwnerForCreate`: the caller email for everyone, including admins.
- `FilterOwners(requested)`: the requested owners for admins, `[caller]` for everyone else.

Rules:

- The owner filter passed to a repository is `currentUser.Owner.VisibleOwner` or
  `currentUser.Owner.FilterOwners(requested)`, never a raw body/query value.
- An admin-only `createdBy` filter is allowed only when `Owner.IsAdmin`; non-admins are pinned to their own
  email in `ToInput`.
- A non-admin reading/updating/deleting another owner's row gets the same answer as a missing row (`404` or a
  `false`/empty result), not a `403` that confirms the row exists.
- The stored row's `CreatedBy` is the owner for update-time uniqueness checks; an admin updating another user's
  row checks against that user's names/titles.
- Audit fields (`CreatedBy`, `UpdatedBy`) come from the token (`Owner.Email`), not from the request.
- `OwnerScopeTests` covers the three methods; `HttpContextCurrentUserTests` covers the claim adapter.

## Adding a role or policy

1. Add the policy/role constant to `Api/Configuration/Constants.cs` and include it in
   `AuthorizationPolicies` if it is a first-class policy.
2. Add the policy in `ConfigureServices.ConfigureAuth` with `RequireRole(...)` and the email assertion.
3. Map the new route group or endpoint with the narrowest policy in `CreateAuthorizedRouteGroupBuilder`.
4. Add the role to `TestUser`/`TestUsers` for the test identities that need it; keep the existing identities.
5. Add a profile to `local/tools/devtoken.json` and a user to `local/openapi/users.json` (same email/role) so
   local development can exercise it.
6. Add the authorization matrix tests: anonymous `401`, authenticated without role `403`, wrong role `403`,
   allowed role `2xx`, and (for owner-scoped routes) another owner's row.
7. Update the API table in `README.template.md` / `README.md`.

**Done when:** the policy has a test for every admitted role and one for a rejected role; local profiles and
test users use the same role strings; the OpenAPI description names the policy.

## Test identities

`TestUser` covers one identity per role plus two negative cases:

| `TestUser` | Role | Expected on `User` routes | Expected on `Operator` routes |
|---|---|---|---|
| `TodoUser` | `User` | 2xx | 403 |
| `TodoOperator` | `Operator` | 2xx | 2xx |
| `TodoAdmin` | `Admin` | 2xx | 2xx |
| `TodoExternal` | `External` | 403 | 403 |
| `AuthenticatedWithoutRoles` | — | 403 | 403 |
| `AuthenticatedWithoutEmail` | `User` | 403 (policy email assertion) | 403 |

The names carry `Todo` because they are the sample identities; when the sample is removed, rename them to the
new feature if the user wants, but keep one identity per role and the two negative cases. `TestUsers.Email`
returns the email stored in `CreatedBy`/`UpdatedBy`.

`TestTokenIssuer` signs tokens per run with a random key. It never writes a token or key to disk and never
calls a login endpoint. The applications receive only validation material through `AppHostContract.JwtTrust`.

## Local tokens

- `dotnet local/tools/devtoken.cs init` creates the HS256 key in the AppHost user secrets.
- `devtoken list` shows profiles; `devtoken get --profile user` prints a token on stdout.
- The local Swagger sidecar mints the same tokens for its profile picker from `local/openapi/users.json`.
- The issuer/audience come from `appsettings.json` and the AppHost, not from the token.
- Never commit a token or key; never paste one into a report. The dashboard URL contains a login token: do not
  share it.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| 401 on every authenticated test | Trust mapping (issuer/audience/key) or claim types differ from what the app validates. Check `TestAuthSettings` and `AppHostContract.JwtTrust`. |
| 403 where success was expected | Token accepted; role or claim type/value does not match the policy, or the email claim is missing. |
| 200 where 403 was expected | The route group uses a broader policy, or the endpoint is not in the group. |
| A user sees another owner's row | `Owner.VisibleOwner` was not passed to the repository, or the repository ignored `createdBy`. |
| Admin cannot see other owners | `Owner.VisibleOwner` is `null` and the admin `createdBy` filter is dropped; check `ToInput` and `FilterOwners`. |
| `JwtBearer scheme '…' would download metadata` | `Authority`/`MetadataAddress` not cleared in the test mapping. |
| Local tokens rejected after a restart | The key was rotated; restart the AppHost and select a profile again (`devtoken init --rotate` changes it). |
