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
    }

    /// <summary>Configuration keys exactly as the API reads them (":"-separated).</summary>
    public static class AppSettings
    {
        /// <summary>Connection string NAME read by the API (ConnectionStrings:Postgres).</summary>
        public const string PostgresConnectionName = "Postgres";

        /// <summary>JWT signing key read by the API (CleanWebApiTemplate.Domain.Configuration.Constants.API_KEY).</summary>
        public const string ApiKey = "API_KEY";

        /// <summary>OIDC authority. The local environment clears it: validation uses the local key only.</summary>
        public const string Authority = "Authentication:Authority";

        /// <summary>Audience the API validates. Supplied per run by <see cref="JwtTrust"/>.</summary>
        public const string Audience = "Audience";

        /// <summary>Issuers the API accepts (array read as <c>ValidIssuers:0</c>).</summary>
        public const string ValidIssuers = "ValidIssuers:0";
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
    /// authority, the signing key, and the issuer and audience of this run.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, TrustValue>> JwtTrust(string resourceName) => resourceName switch
    {
        Resources.Api =>
        [
            new(AppSettings.Authority, TrustValue.Empty),
            new(AppSettings.ApiKey, TrustValue.SigningKeyText),
            // Issuer and audience come from configuration, so local development and every test run
            // can use different values without touching the application code.
            new(AppSettings.ValidIssuers, TrustValue.Issuer),
            new(AppSettings.Audience, TrustValue.Audience),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No JWT trust mapping for this resource."),
    };

    public static string ToEnvironmentVariable(string configurationKey) => configurationKey.Replace(":", "__");
}
