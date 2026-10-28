namespace CleanWebApiTemplate.AppHost;

/// <summary>
/// Single source of truth shared by the AppHost and, in the future, the test fixtures: run modes, resource names,
/// the configuration keys the applications read, and how the API trusts the locally signed JWTs.
/// </summary>
#if APPHOST
internal
#else
public
#endif
static class AppHostContract
{
    /// <summary>Configuration key that selects the mode. Absent means <see cref="Modes.Local"/>.</summary>
    public const string ModeKey = "RunMode";

    public static class Modes
    {
        /// <summary>`aspire run`: dashboard, telemetry, launch profiles, schema + synthetic seed, local JWT keys.</summary>
        public const string Local = "Local";

        /// <summary>Component tests: infrastructure and schema only; the API runs in-process (WebApplicationFactory).</summary>
        public const string Component = "Component";

        /// <summary>Functional tests: infrastructure, schema and the API as a black box (explicit start).</summary>
        public const string Functional = "Functional";
    }

    /// <summary>Test modes: proxy that receives every outbound HTTP(S) call of the applications.</summary>
    public const string OutboundProxyKey = "Testing:OutboundProxy";

    /// <summary>Aspire resource names. Lowercase and stable: the devtoken configuration and the docs use them.</summary>
    public static class Resources
    {
        public const string Postgres = "postgres";

        /// <summary>Database resource. It is referenced with connectionName "Postgres" (the key the API reads).</summary>
        public const string PostgresDb = "postgresdb";

        public const string DbInitializer = "db-init";
        public const string Api = "api";

        /// <summary>Swagger UI enriched with the examples catalog in local/openapi. Local mode only.</summary>
        public const string Swagger = "swagger";

        /// <summary>Ephemeral secrets server: OpenBao in dev mode, in memory, no volume.</summary>
        public const string OpenBao = "openbao";

        /// <summary>One-shot job that writes the application's secrets into OpenBao before the application starts.</summary>
        public const string OpenBaoInitializer = "openbao-init";
    }

    /// <summary>Configuration keys exactly as the API reads them (":"-separated).</summary>
    public static class AppSettings
    {
        /// <summary>Connection string NAME read by the API (ConnectionStrings:Postgres).</summary>
        public const string PostgresConnectionName = "Postgres";

        /// <summary>JWT signing key read by the API. The AppHost seeds it into OpenBao instead of passing it as an environment variable.</summary>
        public const string ApiKey = "API_KEY";

        /// <summary>OIDC authority. The local environment clears it: validation uses the local key only.</summary>
        public const string Authority = "Authentication:Authority";

        /// <summary>Audience the API validates. Supplied per run by <see cref="JwtTrust"/>.</summary>
        public const string Audience = "Audience";

        /// <summary>Issuers the API accepts (array read as <c>ValidIssuers:0</c>).</summary>
        public const string ValidIssuers = "ValidIssuers:0";
    }

    /// <summary>
    /// Configuration the local Swagger sidecar reads to mint the same JWT as devtoken. The signing key
    /// is the same text seeded into OpenBao as API_KEY; the issuer, audience and role claim are the
    /// values the API validates.
    /// </summary>
    public static class LocalLogin
    {
        public const string SigningKey = "LocalAuth__SigningKey";
        public const string Issuer = "LocalAuth__Issuer";
        public const string Audience = "LocalAuth__Audience";
        public const string RoleClaim = "LocalAuth__RoleClaim";
        public const string LifetimeMinutes = "LocalAuth__LifetimeMinutes";
    }

    /// <summary>
    /// OpenBao: the secrets store the applications read at startup, in every mode. The application never receives the
    /// secret values as environment variables; it receives how to reach the store and its token.
    /// </summary>
    public static class OpenBao
    {
        /// <summary>Base address of the server.</summary>
        public const string AddressKey = "OpenBao:Address";

        /// <summary>Token sent in <c>X-Vault-Token</c>.</summary>
        public const string TokenKey = "OpenBao:Token";

        /// <summary>KV v2 mount point.</summary>
        public const string MountKey = "OpenBao:Mount";

        /// <summary>Path of the secret that holds the application's configuration.</summary>
        public const string PathKey = "OpenBao:Path";

        /// <summary>KV v2 mount point configured in the container.</summary>
        public const string Mount = "secret";

        /// <summary>AppHost configuration entry a test uses to supply its own root token.</summary>
        public const string RootTokenParameter = "openbao-root-token";

        public static string RootTokenParameterKey => $"Parameters:{RootTokenParameter}";

        /// <summary>One secret path per mode: parallel runs never share values.</summary>
        public static string SecretsPath(string mode) => $"cleanwebapitemplate/{mode.ToLowerInvariant()}";
    }

    /// <summary>
    /// Configuration the AppHost reads to make the API trust the local issuer. Local mode reads it from
    /// local/tools/devtoken.json and the AppHost user secrets (written by `devtoken init`).
    /// </summary>
    public static class LocalAuth
    {
        public const string AlgorithmKey = "LocalAuth:Algorithm";
        public const string IssuerKey = "LocalAuth:Issuer";

        /// <summary>HS256 secret (text). The AppHost passes it as a secret parameter.</summary>
        public const string SigningKeyParameter = "local-jwt-signing-key";

        /// <summary>RS256 public key (PEM). The private key never reaches the AppHost or the applications.</summary>
        public const string PublicKeyParameter = "local-jwt-public-key";

        public static string AudienceKey(string resourceName) => $"LocalAuth:Apis:{resourceName}:Audience";

        public static string RoleClaimKey(string resourceName) => $"LocalAuth:Apis:{resourceName}:RoleClaim";

        public static string ParameterKey(string parameterName) => $"Parameters:{parameterName}";
    }

    /// <summary>A value of the local JWT trust, resolved per mode (AppHost) or per run (component fixture).</summary>
    public enum TrustValue
    {
        Issuer,
        Audience,
        /// <summary>HS256 key as the app reads it (UTF-8 text).</summary>
        SigningKeyText,
        /// <summary>HS256 key bytes in base64.</summary>
        SigningKeyBase64,
        /// <summary>RS256 public key (PEM), for apps that read a public key from configuration.</summary>
        PublicKeyPem,
        /// <summary>Empty string: clears a production value such as Authority so no metadata is downloaded.</summary>
        Empty,
    }

    /// <summary>
    /// How the API trusts the local issuer, written in the configuration keys it already reads: the (cleared)
    /// authority, the issuer and the audience of this run. The signing key is not here: the AppHost seeds it into
    /// OpenBao (see OpenBao.cs), so no secret reaches the application as an environment variable.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, TrustValue>> JwtTrust(string resourceName) => resourceName switch
    {
        Resources.Api =>
        [
            new(AppSettings.Authority, TrustValue.Empty),
            // Issuer and audience come from configuration, so local development and every test run
            // can use different values without touching the application code.
            new(AppSettings.ValidIssuers, TrustValue.Issuer),
            new(AppSettings.Audience, TrustValue.Audience),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No JWT trust mapping for this resource."),
    };

    public static string ToEnvironmentVariable(string configurationKey) => configurationKey.Replace(":", "__");
}
