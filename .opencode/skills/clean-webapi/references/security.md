# Security

JWT authentication in two explicit modes, four role policies, and ownership decided from the token. This
reference covers the wiring, the rules and the test identities.

## Modes

`AuthenticationSettings.Load(configuration, appSettings)` resolves one mode:

| Mode | When | Keys |
|---|---|---|
| **Symmetric** | `Authentication:Mode` is `Symmetric`/`local`, or empty with no authority. Local and tests. | `API_KEY` (HS256, at least 32 UTF-8 bytes); `Authority` must be empty. |
| **Oidc** | `Authentication:Mode` is `Oidc`, or empty with an authority. Deployed. | Absolute HTTPS `Authority`; `API_KEY` is ignored. |

The mode is never mixed: a symmetric key with an authority, or the reverse, fails at startup with a clear
message instead of guessing.

`ConfigureAuth`:

- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`;
- `RequireHttpsMetadata = true`, `MapInboundClaims = true`;
- `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` all true;
- `ValidIssuers` from configuration; in Symmetric mode `IssuerSigningKey` from `API_KEY` and
  `ValidAlgorithms = [HmacSha256]` (the local key is only accepted as HS256);
- in Oidc mode `Authority` is set and the metadata is downloaded over HTTPS.

A token without an `email` claim is authenticated but fails every policy (`HasEmailClaim` assertion) → 403.

## Policies and roles

| Policy | Accepted roles | Intent |
|---|---|---|
| `User` | User, Operator, Admin | Read, create and update the caller's items. |
| `Operator` | Operator, Admin | Delete the caller's items (admins can delete any). |
| `Admin` | Admin | See and act on every owner. |
| `External` | External, Operator, Admin | Reserved for system/third-party integrations; no route uses it yet. |

Policies are registered in `ConfigureAuth` with `RequireRole(...)` plus `RequireAssertion(HasEmailClaim)`.
`Constants.AuthorizationPolicies` lists all four, and `ConstantsTests` asserts the list. A new policy is added
in three places: `Constants`, `ConfigureAuth`, and the test that enumerates the policies. Role names are the
values the tokens carry.

Route groups apply the policy:

```csharp
RouteGroupBuilder userGroup = app.CreateAuthorizedRouteGroupBuilder("todos", "Todos", [Constants.UserPolicy]);
RouteGroupBuilder operatorGroup = app.CreateAuthorizedRouteGroupBuilder("todos", "Todos", [Constants.OperatorPolicy]);
```

Write operations go on the narrowest group that needs them: `DELETE` on `Operator`, the rest on `User`.

## Ownership

`ICurrentUser` is the Application's view of the caller:

```csharp
public interface ICurrentUser
{
    string Email { get; }        // throws when the principal has no email claim
    bool IsAdmin { get; }
    OwnerScope Owner { get; }
}
```

`HttpContextCurrentUser` reads `ClaimTypes.Email` (or the short `email` claim, because `MapInboundClaims` maps
it) and `IsInRole(Constants.AdminPolicy)`. `OwnerScope` turns that into the three questions:

| Member | Result |
|---|---|
| `VisibleOwner` | `null` for admins, the caller's email otherwise. Single-item reads, updates, deletes. |
| `OwnerForCreate` | Always the caller's email, even for admins. |
| `FilterOwners(requested)` | Admins get the requested owners (or none); everyone else is pinned to their own email. |

Rules:

- The owner is **never** read from a request body. `{Verb}{Feature}Request.ToInput(owner)` sets it from the
  `OwnerScope`.
- Non-admins see and change only their own rows. An invisible row is indistinguishable from a missing one:
  `GET`/`PUT` → 404, `DELETE` → 404 (not 403), list endpoints silently scope the filter.
- Admins may read and change any row. Creating always makes the admin the owner.
- An update that changes the title checks uniqueness against the owner of the **stored** row, not the caller.
- `DbContext`-style global filters do not exist here: the scope is passed to the port as
  `string? visibleOwner` (or `Owners` in a filter) and applied in SQL.

Consequences for handlers:

```csharp
TodoEntity? todo = await repository.GetByIdAsync(id, input.VisibleOwner, ct);
if (todo is null)
    return Result<TodoDto>.Failure(new NotFoundError("ID was not found."));
```

`VisibleOwner = null` means "every owner" and is only produced for admins, because the mapper derived it from
`OwnerScope`.

## 401 vs 403

| Situation | Status |
|---|---|
| No token, malformed token, expired, not yet valid, wrong issuer/audience/key/algorithm, unsigned | 401 |
| Valid token without a role the policy accepts | 403 |
| Valid token with the right role but no `email` claim | 403 |
| Visible row missing, or owned by someone else and the caller is not an admin | 404 |
| Delete of an invisible/unknown row | 404 |

The API never returns 403 for ownership: it hides the row.

## Why the email

The owner is the token's `email` claim. It is readable, it matches the devtoken profiles and the seed, and it
is the claim the local sidecar and every OIDC provider can emit. The trade-off is recorded in
`docs/adr/0009-owner-identified-by-email.md`: changing an email in the identity provider orphans that user's
rows, and a provider that omits the claim makes the API return 403. If a deployment needs stable identities,
change the claim mapping in `HttpContextCurrentUser` (the single place) and migrate the columns.

## Test identities

`tests/CleanWebApiTemplate.Tests.Shared/Auth` defines one identity per role plus two negative cases:

| `TestUser` | Role | Expected |
|---|---|---|
| `TodoUser` | User | read/create/update own; delete → 403 |
| `TodoOperator` | Operator | read/create/update/delete own |
| `TodoAdmin` | Admin | everything, any owner |
| `TodoExternal` | External | 403 on every Todo route (no route uses the policy) |
| `AuthenticatedWithoutRoles` | — | 403 on every protected route |
| `AuthenticatedWithoutEmail` | User, no email | 403 before the handler |

Tokens are signed locally per run (`TestTokenIssuer`, HS256) and never come from a login endpoint. The
`InvalidToken` enum lists the eight rejection cases (`Malformed`, `Expired`, `NotYetValid`, `WrongIssuer`,
`WrongAudience`, `SignedWithUnknownKey`, `WrongAlgorithm`, `Unsigned`). The component suite covers all eight;
the functional suite covers all except `WrongAlgorithm` (the API runs as a separate process; the case is
covered in-process).

`local/tools/devtoken.json` defines the same identities for the local environment (`user`, `operator`,
`admin`, `external`, `no-role`) with the same emails as the seed. The Swagger sidecar mints the same tokens
from `local/openapi/users.json`.

## Local tokens

```bash
dotnet local/tools/devtoken.cs init [--rotate]        # HS256 key in the AppHost user secrets
dotnet local/tools/devtoken.cs list                   # profiles, roles and audiences
dotnet local/tools/devtoken.cs get --profile user     # token on stdout, 60 minutes by default
dotnet local/tools/devtoken.cs dotenv .env            # DEVTOKEN_<PROFILE> lines for scripts
```

`--lifetime` accepts 1–1440 minutes. The key never leaves the user-secrets file; the AppHost passes it to
`openbao-init` and to the sidecar, and the application reads it from OpenBao. Do not commit a token, print a
key, or log either one.

## Native AOT and security

The OpenBao client is hand-written because the reflection-based Vault clients break the AOT publish.
`RequireHttpsMetadata = true` is deliberate: metadata over plain HTTP is never allowed, even if a
misconfigured authority points at `http://`.
