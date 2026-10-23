namespace {{AppHostNamespace}};

/// <summary>
/// Single source of truth shared by the AppHost and the test fixtures: run modes, resource names, the configuration
/// keys the applications read, and how each application trusts the locally signed JWTs.
/// </summary>
/// <remarks>
/// Compiled into the AppHost as internal (APPHOST is defined there) and linked as source into the Tests.Shared project,
/// where it is public. Test projects reference both, so they see exactly one public AppHostContract, and nothing under
/// local/ depends on tests/.
/// </remarks>
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

        /// <summary>Functional tests: infrastructure, schema and the applications as black boxes (explicit start).</summary>
        public const string Functional = "Functional";
    }

    /// <summary>
    /// Test modes: proxy that receives every outbound HTTP(S) call of the applications. Default: a dead local port,
    /// so a missed URL fails fast. The functional fixture points it to a recorder and asserts that nothing was sent.
    /// </summary>
    public const string OutboundProxyKey = "Testing:OutboundProxy";

    /// <summary>Aspire resource names. Lowercase and stable: fixtures, devtoken.json and docs use them.</summary>
    public static class Resources
    {
        public const string Postgres = "postgres";
        public const string OrdersDb = "ordersdb";
        public const string DbInitializer = "db-init";
        public const string OrdersApi = "orders-api";
        public const string CatalogApi = "catalog-api";

        // ADAPT (only when used):
        // public const string Redis = "cache";
        // public const string Kafka = "kafka";
        // /// <summary>Third-party HTTP APIs (test modes). Stubs and request log are reset before every test.</summary>
        // public const string WireMock = "wiremock";
        // /// <summary>Loopback identity stub (strategy C only). Never reset during a run.</summary>
        // public const string Identity = "identity";
    }

    // ADAPT (Kafka): topics the fixture creates before the app starts (only those the app does not create itself).
    // public static class Topics
    // {
    //     public const string Orders = "orders";
    //     public static readonly string[] CreatedByFixture = [Orders];
    // }

    /// <summary>Configuration keys exactly as the applications read them (":"-separated).</summary>
    public static class AppSettings
    {
        /// <summary>Connection string NAME read with GetConnectionString("OrdersDb").</summary>
        public const string OrdersDbConnectionName = "OrdersDb";

        // ADAPT: every other key the AppHost or the component fixture sets, e.g.
        // public const string KafkaBootstrapServers = "Kafka:BootstrapServers";
        // public const string PaymentsApiBaseUrl = "PaymentsApi:BaseUrl";
    }

    /// <summary>
    /// Configuration the AppHost reads to make the applications trust the local issuer. Local mode reads it from
    /// local/tools/devtoken.json and the AppHost user secrets (written by `devtoken init`); test modes receive it as
    /// AppHost arguments from the fixture, with an ephemeral key per run.
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
        /// <summary>HS256 key as the app reads it (UTF-8 text, e.g. Jwt:Key).</summary>
        SigningKeyText,
        /// <summary>HS256 key bytes in base64 (Authentication:Schemes:{scheme}:SigningKeys:n:Value).</summary>
        SigningKeyBase64,
        /// <summary>RS256 public key (PEM), for apps that read a public key from configuration.</summary>
        PublicKeyPem,
        /// <summary>Empty string: clears a production value such as Authority so no metadata is downloaded.</summary>
        Empty,
    }

    /// <summary>
    /// ADAPT: how each application trusts the local issuer (strategies A/B of references/local-jwt-authentication.md),
    /// written in the configuration keys it ALREADY reads (discovery).
    /// The same mapping is applied by the AppHost (environment variables, Local and Functional modes) and by the
    /// component fixture (in-process settings), so the three modes cannot drift apart. No production code changes.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, TrustValue>> JwtTrust(string resourceName) => resourceName switch
    {
        // Validation fixed in code: SymmetricSecurityKey(UTF8(Jwt:Key)), ValidIssuer = Jwt:Issuer, ValidAudience = Jwt:Audience.
        Resources.OrdersApi =>
        [
            new("Jwt:Issuer", TrustValue.Issuer),
            new("Jwt:Audience", TrustValue.Audience),
            new("Jwt:Key", TrustValue.SigningKeyText),
        ],

        // Config-bound JwtBearer ("Authentication:Schemes:Bearer") with a production Authority: clear it (no metadata
        // download) and supply issuer, audience and signing key explicitly.
        Resources.CatalogApi =>
        [
            new("Authentication:Schemes:Bearer:Authority", TrustValue.Empty),
            new("Authentication:Schemes:Bearer:ValidIssuer", TrustValue.Issuer),
            new("Authentication:Schemes:Bearer:ValidAudiences:0", TrustValue.Audience),
            new("Authentication:Schemes:Bearer:SigningKeys:0:Issuer", TrustValue.Issuer),
            new("Authentication:Schemes:Bearer:SigningKeys:0:Value", TrustValue.SigningKeyBase64),
        ],

        _ => throw new ArgumentOutOfRangeException(nameof(resourceName), resourceName, "No JWT trust mapping for this resource."),
    };

    public static string ToEnvironmentVariable(string configurationKey) => configurationKey.Replace(":", "__");
}
