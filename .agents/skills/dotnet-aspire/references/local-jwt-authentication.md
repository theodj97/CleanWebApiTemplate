# Local JWT authentication and authorization (local development and tests)

Canonical for SKILL.md rule 9 (*new rule*). Every protected service is run and tested with **JWTs signed locally**,
validated with explicit keys and parameters, with no identity provider and no network call. Templates:
`assets/local/tools/devtoken.cs`, `assets/local/tools/devtoken.json`, `assets/local/apphost/AppHostContract.cs`
(trust mapping), `assets/tests/shared/Auth/*`, `assets/tests/component/ComponentTestFixture.cs`,
`assets/tests/functional/FunctionalTestFixture.cs`, `assets/tests/shared/Http/OutboundCallRecorder.cs`,
`assets/tests/component/AuthenticationTests.cs`.

## Contents
- [The rule](#the-rule)
- [Every protected service is treated the same](#every-protected-service-is-treated-the-same)
- [Choosing the trust strategy per application](#choosing-the-trust-strategy-per-application)
- [Trust mapping](#trust-mapping)
- [Algorithms, issuers, audiences and validation](#algorithms-issuers-audiences-and-validation)
- [Claims the application reads](#claims-the-application-reads)
- [Local development: devtoken](#local-development-devtoken)
- [Key storage](#key-storage)
- [Tests: TestTokenIssuer and test users](#tests-testtokenissuer-and-test-users)
- [Trust in component tests](#trust-in-component-tests)
- [Trust in functional tests](#trust-in-functional-tests)
- [Last resort: loopback identity stub](#last-resort-loopback-identity-stub)
- [Microsoft.Identity.Web](#microsoftidentityweb)
- [Applications that do not use JWT](#applications-that-do-not-use-jwt)
- [Isolation from production](#isolation-from-production)
- [Authentication and authorization tests](#authentication-and-authorization-tests)
- [Proving that no identity provider is contacted](#proving-that-no-identity-provider-is-contacted)

## The rule

No authentication or authorization flow used for local development or tests may depend on an identity provider or
any external service. No exceptions for convenience. After package restore, starting and running the applications
and the tests needs no HTTP call to obtain, discover or validate credentials. Restoring packages to build is not a
runtime dependency.

Forbidden in Local, Component and Functional modes:

- external login or sign-in services, remote OAuth 2.0/OpenID Connect providers to obtain tokens, remote metadata
  discovery, remote JWKS downloads, token exchange services;
- an `Authority` or `MetadataAddress` that makes JwtBearer download anything from a provider (clear it, see
  [Trust mapping](#trust-mapping));
- developer credentials, tokens copied from another environment, production secrets, Internet connectivity as a
  condition for authentication to work;
- unsigned tokens, "accept any token" handlers, test authentication schemes that replace JwtBearer, disabled
  validation (signature, issuer, audience, lifetime), environment-based bypasses that open protected endpoints, and
  changes to authorization policies so that local runs or tests pass.

The goal is to replace trust in the provider by an isolated local cryptographic configuration, never to remove
authentication. A configuration found to break this rule is fixed and verified, not just documented (SKILL.md
Phase 6); when the fix is production code, it is reported with evidence and needs consent (rule 6).

## Every protected service is treated the same

- Applies to every service that validates JWTs: HTTP APIs, gRPC services, SignalR hubs (token in the `access_token`
  query string), workers that validate tokens. Each one runs locally and is tested with locally signed JWTs through
  the same mechanism.
- Do not decide the architecture by looking for login endpoints, and never use an application's login endpoint to
  obtain development or test tokens for itself or for other services.
- Reading the code that **issues** tokens (a `JwtService`, a login handler) is allowed and is the best evidence of
  the claim types, issuer and audience the application expects.
- An existing login endpoint keeps working: in Local and test modes it signs with the local key (it reads the same
  configuration), and it is tested as an ordinary endpoint (`LoginEndpointTests` in the sample).

## Choosing the trust strategy per application

From discovery (`discovery.md` § 6), decide for each protected application, in this order, and record the decision in
the plan and the final report:

| # | Evidence in the application | Strategy | Production change |
|---|---|---|---|
| A | Signing key read from configuration (`IssuerSigningKey = new SymmetricSecurityKey(UTF8(config["Jwt:Key"]))`, a public key PEM or certificate from configuration), issuer/audience from configuration | Supply a local key, issuer and audience through those keys | none |
| B | Config-bound JwtBearer (`AddJwtBearer()` reading `Authentication:Schemes:{Scheme}`), `Authority` in configuration | Clear `Authority` (and `MetadataAddress`), supply `ValidIssuer`, `ValidAudiences:n`, `SigningKeys:n:Issuer` and `SigningKeys:n:Value` (base64 of the symmetric key bytes) | none |
| C | Validation only through `Authority`/metadata that configuration cannot clear, Microsoft.Identity.Web, custom `IssuerValidator`/`IssuerSigningKeyResolver` that needs the provider | Ask. Options: (1) loopback identity stub, last resort below; (2) a minimal production change that reads a key from configuration, only with consent | depends |
| D | Issuer, audience or key hard-coded in code | Ask: any option is a production change. Never edit it on your own | yes |

Verified (2026-10-23): strategy A on an API that signs and validates with `Jwt:Key` (HS256, login endpoint) and
strategy B on a validation-only API with a production `Authority` in `appsettings.json` both worked in Local,
Component and Functional modes with no outbound call. The real repositories inspected while designing this skill all
fall in A (HS256 key in configuration, validation fixed in code).

## Trust mapping

The decision becomes data in the contract, `AppHostContract.JwtTrust(resource)`: a list of
`(configuration key the app already reads, TrustValue)`. The same mapping is applied everywhere, so the three modes
cannot drift apart and no production code changes:

- Local and Functional modes: the AppHost turns it into environment variables (`WithLocalJwtTrust`), resolving values
  from the mode's local JWT configuration (key as a secret parameter);
- Component mode: the fixture turns it into in-process settings (`TestTokenIssuer.TrustSettings`).

| `TrustValue` | Resolves to |
|---|---|
| `Issuer` | the mode's issuer (`LocalAuth:Issuer`) |
| `Audience` | the mode's audience for that application (`LocalAuth:Apis:{resource}:Audience`) |
| `SigningKeyText` | HS256 key as text (apps doing `Encoding.UTF8.GetBytes(config["Jwt:Key"])`) |
| `SigningKeyBase64` | base64 of the HS256 key bytes (config-bound `SigningKeys:n:Value`) |
| `PublicKeyPem` | RS256 public key (apps that read one from configuration) |
| `Empty` | empty string: clears `Authority`/`MetadataAddress` from appsettings |

```csharp
public static IReadOnlyList<KeyValuePair<string, TrustValue>> JwtTrust(string resourceName) => resourceName switch
{
    Resources.OrdersApi =>                                    // strategy A
    [
        new("Jwt:Issuer", TrustValue.Issuer),
        new("Jwt:Audience", TrustValue.Audience),
        new("Jwt:Key", TrustValue.SigningKeyText),
    ],
    Resources.CatalogApi =>                                   // strategy B
    [
        new("Authentication:Schemes:Bearer:Authority", TrustValue.Empty),
        new("Authentication:Schemes:Bearer:ValidIssuer", TrustValue.Issuer),
        new("Authentication:Schemes:Bearer:ValidAudiences:0", TrustValue.Audience),
        new("Authentication:Schemes:Bearer:SigningKeys:0:Issuer", TrustValue.Issuer),
        new("Authentication:Schemes:Bearer:SigningKeys:0:Value", TrustValue.SigningKeyBase64),
    ],
    _ => throw new ArgumentOutOfRangeException(nameof(resourceName)),
};
```

Override exactly what the production configuration sets: if appsettings has `ValidIssuers` (array) instead of
`ValidIssuer`, map `ValidIssuers:0`; if it lists several audiences, override each index you need. Config-bound
JwtBearer only accepts symmetric `SigningKeys` and only keeps keys whose `Issuer` is one of the valid issuers.

An empty environment variable from the AppHost cleared `Authority` on Linux (verified). It was not verified on Windows
or macOS: check it on the platforms the team uses. The functional recorder below fails loudly if `Authority` survives
an environment variable (the component guard checks in-process settings, which always carry empty values).

## Algorithms, issuers, audiences and validation

- **Asymmetric whenever viable** (RS256): the issuer keeps the private key; applications receive only the public key
  (`PublicKeyPem`). Viable only when the application reads a public key from configuration (strategy A with a key PEM,
  or the loopback stub of strategy C).
- **HS256 when the application only accepts a symmetric key through configuration** (strategies A with `Jwt:Key`, B).
  This is not a weakening: the key is a random 512-bit secret per environment (local machine, test run), never shared
  with production, and the application already holds its validation key in production too. With HMAC the validation
  material is the signing material, so the "public key only" property cannot exist: record it as a documented
  limitation, not a reason to change production code.
- **Issuers and audiences are per environment** when the application reads them from configuration: e.g.
  `urn:{app}:local-dev` / `urn:{app}:tests` and `urn:{app}:{api}:local-dev` / `…:tests`, never production values, so a
  token of one environment is invalid in every other. If the application pins them in code, keep the pinned values,
  rely on the per-environment key, and report it.
- **Validation stays on**: signature, issuer, audience, lifetime (`exp`/`nbf`, default 5-minute clock skew), the
  algorithms the application allows (`ValidAlgorithms` if set), and the application's policies, handlers, role and
  scope checks, exactly as in production. Never set `ValidateIssuer=false`, `RequireSignedTokens=false`,
  `SignatureValidator` that skips checks, or similar, in any mode.

## Claims the application reads

Discover the real mapping in the code; do not assume every application uses the same claim names.

- JwtBearer default (`MapInboundClaims = true`): `role` and `roles` become `ClaimTypes.Role` (default `RoleClaimType`),
  `sub` → `ClaimTypes.NameIdentifier`, `email` → `ClaimTypes.Email`, `scp` →
  `http://schemas.microsoft.com/identity/claims/scope`; `name` is not mapped. Emit the short names and let the mapping
  happen, as with the real provider.
- `MapInboundClaims = false` (or a handler without mapping): claims keep their names; emit the exact
  `TokenValidationParameters.RoleClaimType` (often `roles` with Entra ID) and the scope claim the policies read
  (`scp`, space separated, or `scope`).
- `IClaimsTransformation` that loads roles from the database: roles come from data, so the user rows must exist after
  every reset (`SeedPersistentDataAsync`, `database.md`).
- Per application, these claim types go to `devtoken.json` (`Apis.{resource}.RoleClaim/ScopeClaim`) and to
  `TestAuthSettings.For(resource)`.

## Local development: devtoken

`local/tools/devtoken.cs` is a single-file .NET 10 app (no project, no NuGet package, no global install) that signs
JWTs locally for the profiles in `local/tools/devtoken.json`. It never calls an endpoint. Copy the asset unchanged;
adapt only `devtoken.json`.

```bash
dotnet local/tools/devtoken.cs init                 # once per machine: creates the local key (idempotent; --rotate replaces it)
dotnet local/tools/devtoken.cs list                 # profiles, their API, roles and scopes
dotnet local/tools/devtoken.cs get --profile <name> [--lifetime <minutes>]
```

- The token is the only output on stdout; diagnostics go to stderr and never contain a token or key; exit code 1 on
  errors (unknown profile, missing key, bad lifetime), 2 on usage errors. On Linux/macOS `chmod +x` lets
  `./local/tools/devtoken.cs get …` work through the shebang.
- `devtoken.json` (committed, no secrets): `AppHostProject` (relative path; its `UserSecretsId` locates the key),
  `Algorithm` (`HS256`/`RS256`, from the trust strategies), `Issuer`, `DefaultLifetimeMinutes`, `Apis` (per resource
  name: `Audience`, `RoleClaim`, `ScopeClaim`) and `Profiles` (per name: `Api`, `Subject`, `Name`, `Roles`, `Scopes`,
  `Claims`). The AppHost loads the same file in Local mode, so issuer and audiences cannot drift.
- Profiles come from the real roles, scopes, claims and policies found in discovery: one per role (per API), one per
  role/claim combination a policy requires, and at least one authenticated profile without permissions per API to
  check 403. Never invent `admin`/`reader`-style profiles the application does not know. Subjects may match seeded
  users so tokens and seed data refer to the same people.
- Using a token from any client:
  ```bash
  TOKEN=$(dotnet local/tools/devtoken.cs get --profile orders-admin)
  curl -H "Authorization: Bearer $TOKEN" https://localhost:7180/orders
  ```
  ```powershell
  $token = dotnet local/tools/devtoken.cs get --profile orders-admin
  Invoke-RestMethod https://localhost:7180/orders -Headers @{ Authorization = "Bearer $token" }
  ```
  Swagger/Scalar "Authorize", Bruno or `.http` files: paste the value or read it from an environment variable. Never
  commit a token into a collection.
- SDK older than .NET 10 (`global.json`): create `local/tools/{App}.DevToken/` as a console project with the same
  source in `Program.cs` (`dotnet run --project local/tools/{App}.DevToken -- get --profile …`), keep it out of every
  deployment, and add it to the solution's `local` folder.

## Key storage

- Local keys live in the **AppHost user secrets** (`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows,
  `~/.microsoft/usersecrets/<id>/secrets.json` elsewhere): outside the repository, per user, the store
  `dotnet user-secrets` and `aspire secret` use. `devtoken init` creates the directory with mode 700 and the file with
  600 on Linux/macOS.
- HS256: `Parameters:local-jwt-signing-key` (a secret parameter in the AppHost). RS256:
  `Parameters:local-jwt-public-key` for the AppHost and `LocalAuth:PrivateKeyPem`, which only devtoken reads.
- Created explicitly (`init`), kept across restarts (no re-creation on each start, so tokens and configuration stay
  valid), replaced only with `--rotate`. In Local mode the AppHost fails fast with the `init` command when the key is
  missing.
- Test keys never touch disk: `TestTokenIssuer` creates them per run in memory.
- No key, token or secret goes into the repository, sample configuration, scripts or documentation, so no
  `.gitignore` change is needed.

## Tests: TestTokenIssuer and test users

- `TestUser` (enum) is generated from discovery: one user per role, one per role/claim combination a policy requires
  (e.g. role `Catalog.Editor` without scope `catalog.write`), and `AuthenticatedWithoutRoles` (no roles, no scopes) to
  prove 403. Anonymous needs no user. Why an enum: the compiler checks the user exists and enums work in attributes and
  `InlineData`.
- `TestUsers` gives each user fixed subject, name, roles, scopes and additional claims (tenant, `oid`, custom claims),
  so data linked to a user is predictable and `SeedPersistentDataAsync` can insert matching rows.
- `TestAuthSettings`: the tests' issuer and algorithm, the list of applications that trust the issuer, and per
  application its audience and claim types (`For(resource)`), different from local and production values whenever the
  application reads them from configuration; values pinned in code are reused and reported (§ Algorithms, issuers,
  audiences and validation). *Changed:* the former rule reused the application's pinned `ValidIssuer` by default.
- `TestTokenIssuer` (*changed*: the former one signed RS256 only and created valid tokens only): a new HS256 secret or
  RSA key per run, in memory, with the algorithm the trust strategy allows. `CreateToken(user, resource)` shapes the token
  for that application; `CreateInvalidToken(kind, user, resource)` creates the negative cases (malformed, expired and
  not yet valid beyond the clock skew, wrong issuer, wrong audience, signed with an unknown key, unsigned `alg: none`);
  `TrustSettings(resource)` and `AppHostArguments()` hand the validation material to the application.
- Lifetime 8 hours, `nbf` 5 minutes in the past: tokens never expire mid-run and tolerate clock differences. If the
  application validates lifetimes against a fake `TimeProvider`, start the fake clock near the real time
  (`integration-testing.md` § Asynchrony, time and determinism).
- Choosing the identity in a test: `integration-testing.md` § Identity.

## Trust in component tests

Default: configuration. The fixture puts `TokenIssuer.TrustSettings(resource)` into the factory settings with the
infrastructure settings (`UseSetting` for values read in `Program.cs` before `Build`, plus a last in-memory source),
checks after startup that the application reads every test value, and runs **`EnsureNoRemoteTokenMetadataAsync`**:
every JwtBearer scheme must end with no `Authority`, no `MetadataAddress` and no remote `ConfigurationManager`, or the
run fails with the offending address (*new*, verified by removing the `Authority` override on purpose).

Fallback, only for strategy C apps whose validation configuration cannot be reached (*changed*: it was the default of
the former testing skill): adjust `JwtBearerOptions` in-process from `ComponentAppFactory.ConfigureTestServices`:

```csharp
// tokenIssuer: the fixture's TestTokenIssuer (passed to the factory); audience: TestAuthSettings.For(<resource>).Audience.
// *All variants: every JwtBearer scheme the application registers.
services.ConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);
services.PostConfigureAll<JwtBearerOptions>(TrustTestTokenIssuer);

void TrustTestTokenIssuer(JwtBearerOptions options)
{
    var configuration = new OpenIdConnectConfiguration { Issuer = tokenIssuer.Issuer };
    configuration.SigningKeys.Add(tokenIssuer.SigningKey);

    // No authority and a static configuration: the handler never downloads metadata.
    options.Authority = null;
    options.MetadataAddress = string.Empty;
    options.RequireHttpsMetadata = false;
    options.Configuration = configuration;
    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);

    var validation = options.TokenValidationParameters;
    validation.ValidateIssuer = true;
    validation.ValidIssuer = tokenIssuer.Issuer;
    validation.ValidIssuers = [tokenIssuer.Issuer];
    validation.ValidateAudience = true;
    validation.ValidAudience = audience;
    validation.ValidAudiences = [audience];
    validation.ValidateIssuerSigningKey = true;
    validation.IssuerSigningKey = tokenIssuer.SigningKey;
    validation.IssuerSigningKeys = [tokenIssuer.SigningKey];
    validation.ValidateLifetime = true;

    // Custom validators that need the real provider (e.g. Microsoft.Identity.Web's issuer validator) cannot validate
    // self-issued tokens. Role/claim mapping (RoleClaimType, MapInboundClaims) is kept: it is the behaviour under test.
    validation.IssuerValidator = null;
    validation.IssuerSigningKeyResolver = null;
}
```

Why both `Configure` and `PostConfigure`: the framework's `JwtBearerPostConfigureOptions` runs before a test
`PostConfigure` and, when `ConfigurationManager` is still null, builds one that downloads metadata from `Authority`
(and throws for a non-HTTPS authority while `RequireHttpsMetadata` is true); a `Configure` runs before it and sets
`Configuration`, so the framework wraps it in a static manager; the `PostConfigure` runs after every registration of
the application (including libraries that post-configure) and re-asserts the values. The metadata guard still runs.
That application still needs a decision for Functional and Local modes.

## Trust in functional tests

The application is a separate process configurable only from outside. The fixture passes
`TokenIssuer.AppHostArguments()` (algorithm, issuer, audiences of every trusting application, the key) to the
AppHost, which applies the contract mapping as environment variables, exactly as in Local mode. The proxy variables
point to an `OutboundCallRecorder` owned by the fixture: every outbound call is refused and recorded, and the
valid-token test asserts that none happened.

## Last resort: loopback identity stub

*Changed:* this was the default functional trust of the former testing skill; explicit keys through the trust mapping
now come first. Only for strategy C, after asking, and only in the AppHost's own loopback resources: a WireMock resource named
`identity` serves `/.well-known/openid-configuration` and `/.well-known/jwks.json` with the RS256 public key
(`IdentityProviderStub.RegisterAsync` with `TestTokenIssuer.GetDiscoveryDocumentJson`/`GetJwksJson`, registered by the
fixture before it starts the applications). The application's `Authority`/`MetadataAddress` points to that resource
with `RequireHttpsMetadata=false`. It is never reset during a run and is separate from the third-party WireMock. In
Local mode the same stub needs devtoken's RS256 public key: implement it only if the user accepts the extra resource;
it is then the only WireMock resource allowed in Local mode (`external-services.md` keeps third parties real there).
It is still a local cryptographic setup (no provider, no Internet), but it is an HTTP metadata download, which is why
explicit keys come first. *Removed:* the former option of an identity-provider container (Keycloak) that issues the
tokens is incompatible with this rule.

## Microsoft.Identity.Web

- Web APIs reject tokens with neither `scp` nor `roles` (`IDW10201`): give every test user and devtoken profile a
  scope claim, including the users without roles, so they reach authorization and get 403.
- Configuration lives in `AzureAd` (`Instance`, `TenantId`, `ClientId`, `Audience`); the authority is
  `{Instance}{TenantId}/v2.0`. Its issuer validation is specific to Entra ID. This is usually strategy C: the stub
  then needs `/{tenant}/v2.0/.well-known/openid-configuration`, `Instance` pointing to it and a `tid` claim equal to
  `TenantId`; in component tests the fallback clears `IssuerValidator`. If the library rejects a non-HTTPS or
  non-Microsoft authority, ask.

## Applications that do not use JWT

The goal stays the same — every test chooses its identity explicitly, no external provider — and the decision goes
into the summary.

- **Cookies**: log in through the application's real login endpoint with seeded test users and keep one
  cookie-carrying client per user (`CreateClient(user)` logs in once and caches the cookie). A test authentication
  scheme is acceptable in component tests only when the login flow itself depends on an external provider.
- **API keys**: one key per `TestUser`, seeded in the database or injected through configuration; `CreateClient(user)`
  adds the header.

## Isolation from production

- Production configuration (`appsettings.json`, `appsettings.Production.json`, deployment settings) contains no local
  or test issuer, audience or key; local values reach the application only from the AppHost (Local/Functional) or the
  fixture (Component), which are never deployed. Check: `grep -rn -E 'local-dev|:tests' src` prints nothing.
- devtoken, the AppHost and `local/` are never referenced by `src/` (rule 2) nor published.
- No environment-based switch enables local trust: the application code is unchanged; only configuration values
  differ, and only these two hosts supply them.
- Existing development-only bypasses in the application (`if (env.IsDevelopment())` that allows anonymous access,
  signing keys committed to a base `appsettings.json` that every environment loads) are findings: report them with
  file:line and value redacted, propose the fix, and change production code only with consent.
- Do not register full tokens, private keys or secrets in logs, traces, metrics or exception messages, in the
  applications, the tools or the tests.

## Authentication and authorization tests

For every protected service, adapted to its real endpoints and policies (do not force cases that do not apply):

| Case | Expected |
|---|---|
| Valid local token with sufficient permissions | the endpoint's success status |
| No token | 401 |
| Malformed token, expired, not yet valid, signed with an unknown key, unsigned (`alg: none`) | 401 |
| Wrong issuer / wrong audience (when the scheme validates them) | 401 |
| Authenticated identity without the required role/scope/claim | 403 |
| Each role, scope and custom policy combination on the endpoints that use it | the documented status per identity |
| No call to an identity provider and no remote key download | component guard passes; recorder empty in functional tests |

- Distinguish authentication (401) from authorization (403): never assert "not 200".
- Put the matrix in component tests (`AuthenticationTests` + per-endpoint role `[Theory]`s) and at least the valid,
  anonymous, invalid-token and forbidden cases in functional tests for each application.
- Reading the results: if the allowed identity gets 401, the token is rejected (issuer, audience, key, lifetime,
  trust mapping); if it gets 403, the token is accepted but the role/scope claim type or value does not match.

## Proving that no identity provider is contacted

A JwtBearer metadata download that fails is **not fatal**: validation falls back to the explicit keys and requests
still succeed (verified: with the production `Authority` left in place and a dead proxy, every functional test still
passed). A 200 is therefore not evidence. Use:

- Component: `EnsureNoRemoteTokenMetadataAsync` in the fixture (fails the run).
- Functional: `OutboundCallRecorder` as `HTTP_PROXY`/`HTTPS_PROXY`; the valid-token test asserts
  `Assert.Empty(Fixture.OutboundCalls.Requests)` (it reported `CONNECT login.contoso.example:443` when the override was
  removed on purpose).
- Local: `aspire otel traces <api>` after an authenticated request shows only server spans and the application's own
  dependency spans, no client span to a provider; `aspire describe <api> --format Json` shows the cleared
  `Authority` variable (names only, values redacted).
