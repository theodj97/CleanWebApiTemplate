# Authentication: test users, token issuer, trust, attributes

Templates: `assets/shared/Auth/*` (`TestUser`, `TestUsers`, `TestAuthSettings`, `TestTokenIssuer`,
`IdentityProviderStub`), `assets/shared/Attributes/AuthenticatedAsAttribute.cs`, `UnauthenticatedAttribute.cs`,
`assets/component/ComponentAppFactory.cs` (component trust), `assets/functional/FunctionalTestFixture.cs` and
`assets/apphost/AppHost.cs` (functional trust).

## Contents
- [Test users](#test-users)
- [Role and claim types](#role-and-claim-types)
- [TestTokenIssuer](#testtokenissuer)
- [Trust in component tests](#trust-in-component-tests)
- [Trust in functional tests](#trust-in-functional-tests)
- [Microsoft.Identity.Web](#microsoftidentityweb)
- [Apps that do not use JWT](#apps-that-do-not-use-jwt)
- [Choosing the identity in a test](#choosing-the-identity-in-a-test)
- [Auth smoke tests](#auth-smoke-tests)

## Test users

Generate the `TestUser` enum from discovery section 5:
- one user per role;
- one user per role/claim combination a policy requires (e.g. a policy needing role `Support` and claim `tenant`);
- one authenticated user without roles (`AuthenticatedWithoutRoles`) to prove that protected endpoints answer 403;
- anonymous needs no user: a client without token.

`TestUsers` gives each one fixed, deterministic data: subject/id (`sub`), name, email, roles (exact values the app
checks) and the other claims the app reads (scopes `scp`, tenant `tid`, object id `oid`, custom claims). Fixed subjects
make data linked to a user predictable across runs and let `SeedPersistentDataAsync` insert matching user rows.

Why an enum and not strings: the compiler validates that the user exists, and enum values can be used in attributes
and in `InlineData`.

## Role and claim types

Emit role claims with the exact type that ends up as the app's role claim:

- JwtBearer default (`MapInboundClaims = true`): both `role` and `roles` become `ClaimTypes.Role`, which is the default
  `RoleClaimType` → `[Authorize(Roles = "Admin")]` works with either.
- `MapInboundClaims = false` (or `JsonWebTokenHandler` configured without mapping): the claim keeps its name; use the
  app's `TokenValidationParameters.RoleClaimType` value (often `roles` with Entra ID).
- `IClaimsTransformation` that loads roles from the database: roles come from data, so the user rows (and their roles)
  must exist after every reset (`SeedPersistentDataAsync`).

Other mappings with `MapInboundClaims` on: `sub` → `ClaimTypes.NameIdentifier`, `email` → `ClaimTypes.Email`, `scp` →
`http://schemas.microsoft.com/identity/claims/scope`; `name` is not mapped. If the app reads one of these, emit the
short name and let the mapping happen, exactly as with the real identity provider.

## TestTokenIssuer

- Signs JWTs (RS256) with an RSA key created when the run starts; the key never leaves the process and no secret is
  committed. The key has a `kid`, so the JWKS works like a real provider's.
- Issuer = `TestAuthSettings.Issuer`: the app's pinned `ValidIssuer` if it has one, any stable value otherwise.
  Audience = the app's audience. Lifetime = 8 hours, far beyond a test session; `nbf` 5 minutes in the past to absorb
  clock differences.
- `CreateToken(TestUser)` for tests; `GetJwksJson()` and `GetDiscoveryDocumentJson(authority)` for functional trust.

## Trust in component tests

The app runs in-process, so its `JwtBearerOptions` can be adjusted from `ComponentAppFactory`:

```csharp
services.ConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);
services.PostConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);
```

`TrustTestTokenIssuer` sets `Authority = null`, `MetadataAddress = ""`, `RequireHttpsMetadata = false`, a static
`OpenIdConnectConfiguration` with the issuer and signing key, `ConfigurationManager = new
StaticConfigurationManager<OpenIdConnectConfiguration>(configuration)`, and `TokenValidationParameters` with the test
issuer, audience and key. Why both `Configure` and `PostConfigure`:

- The framework's own post-configuration (`JwtBearerPostConfigureOptions`) runs before a test `PostConfigure` and, when
  `ConfigurationManager` is still null, builds one that downloads metadata from `Authority` — and throws if that
  authority is not HTTPS while `RequireHttpsMetadata` is true. A test `Configure` runs before it and sets
  `Configuration`, so the framework wraps it in a static manager instead.
- The test `PostConfigure` runs after every registration of the app (including libraries that post-configure) and
  re-asserts the same values.

`*All` variants cover every JwtBearer scheme the app registers. Custom validators that need the real provider
(`IssuerValidator`, `IssuerSigningKeyResolver`) are cleared; role mapping (`RoleClaimType`, `MapInboundClaims`) is
kept, because that is the app's behaviour under test.

## Trust in functional tests

The app is a separate process, configurable only from the outside (environment variables set by the AppHost).
Options, in order of preference:

1. **WireMock as identity provider (default).** A dedicated `identity` WireMock resource serves
   `/.well-known/openid-configuration` and `/.well-known/jwks.json` (`IdentityProviderStub.RegisterAsync`, called by
   the functional fixture before it starts the app). The AppHost points the app's `Authority` (or `MetadataAddress`)
   to that resource and sets `RequireHttpsMetadata=false`. The discovery document advertises `TestAuthSettings.Issuer`,
   which equals the tokens' `iss` (and the app's pinned `ValidIssuer`, if any). It is a separate resource from the
   third-party WireMock so per-test resets can never break authentication.
2. **Signing key from configuration.** If the app validates with a key read from configuration (symmetric
   `Jwt:Key`-style setups), generate a key per run, inject it through the AppHost under the app's key name, and change
   only `TestTokenIssuer`: `SigningKey = new SymmetricSecurityKey(bytes)`, credentials with
   `SecurityAlgorithms.HmacSha256`, no JWKS/discovery. `SigningKey` is typed `SecurityKey`, so the component trust code
   and the rest of the infrastructure do not change.
3. **Identity provider container** (e.g. Keycloak via the Aspire Keycloak integration with a realm import that contains
   the test users and roles). `TestTokenIssuer` then obtains tokens from it (password or client-credentials grant)
   instead of signing them. Slowest and most complex: use it only when 1 and 2 are not viable.

If an option needs a production change — e.g. `RequireHttpsMetadata = true` or the authority hard-coded in code — stop
and ask; do not change it on your own. Note the chosen strategy in the final summary.

## Microsoft.Identity.Web

- Web APIs reject tokens that have neither `scp` nor `roles` (`IDW10201`): give every test user a scope claim (the
  template's `ApiScopes`), including `AuthenticatedWithoutRoles`, so that user reaches authorization and gets 403.
- Configuration lives in the `AzureAd` section (`Instance`, `TenantId`, `ClientId`, `Audience`). The authority is
  `{Instance}{TenantId}/v2.0`, so functional trust needs the discovery document at
  `/{tenant}/v2.0/.well-known/openid-configuration` on the identity WireMock (register that path too), `Instance`
  pointing to the WireMock endpoint, and a `tid` claim equal to `TenantId`.
- Its issuer validation is specific to Entra ID: in component tests the template clears `IssuerValidator`; in
  functional tests verify with the smoke tests below and, if the library rejects a non-HTTPS or non-Microsoft
  authority, fall back to option 3 or ask.

## Apps that do not use JWT

The goal stays the same — every test chooses its identity explicitly — and the decision goes into the summary.
- **Cookies**: log in through the app's real login endpoint with seeded test users and keep one cookie-carrying client
  per user (`CreateClient(user)` performs the login once and caches the cookie). In component tests a test
  authentication scheme is an acceptable shortcut only when the login flow depends on an external provider.
- **API keys**: one key per `TestUser`, seeded in the database or injected through configuration; `CreateClient(user)`
  adds the header.

## Choosing the identity in a test

- `[AuthenticatedAs(TestUser.Admin)]` on the method or class for the usual case; the method attribute wins.
- `[Unauthenticated]` cancels a class-level `[AuthenticatedAs]` for one test.
- No attribute → anonymous `Client`. The identity of every call is therefore always visible in the test itself.
- `CreateClient(user)` in Arrange when the attribute cannot express it:
  - a `[Theory]` whose user varies per row (attribute values cannot change per `InlineData` row);
  - a test with several identities (A creates a resource, B tries to read it).

## Auth smoke tests

Wiring mistakes (issuer, audience, role claim type) show up as 401/403 everywhere. After building the infrastructure
add, in component tests (and one in functional tests):
- one protected endpoint called by each role allowed → 2xx;
- the same endpoint called by `AuthenticatedWithoutRoles` → 403;
- the same endpoint called anonymously → 401.
If the "allowed" case returns 401, the token is rejected (issuer, audience, key, lifetime); if it returns 403, the token
is accepted but the role claim type or value does not match.
