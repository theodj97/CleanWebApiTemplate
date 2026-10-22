namespace {{SharedNamespace}};

/// <summary>
/// Single source of truth shared by the test AppHost and the test fixtures: the mode switch, the Aspire resource
/// names, the topics and the configuration keys the application reads. Keeping them in one place stops the AppHost
/// (functional mode) and the component fixture (in-process app) from drifting apart.
/// ADAPT: every value comes from the discovery report — use the exact names the application already reads.
/// </summary>
/// <remarks>
/// This file lives in the Shared project and is LINKED into the test AppHost
/// (<c>&lt;Compile Include="..\..\{App}.Tests.Shared\TestHostContract.cs" Link="TestHostContract.cs" /&gt;</c>, with
/// <c>TEST_APPHOST</c> in the AppHost's DefineConstants). The AppHost's copy is internal, so test projects that reference
/// both Shared and the AppHost see exactly one public TestHostContract, and Shared never references the AppHost.
/// </remarks>
#if TEST_APPHOST
internal
#else
public
#endif
static class TestHostContract
{
    /// <summary>Configuration key the AppHost reads to decide what to start (passed as "--TestMode=...").</summary>
    public const string ModeKey = "TestMode";

    public static class Modes
    {
        /// <summary>Infrastructure only. The app runs in-process inside the component tests (WebApplicationFactory).</summary>
        public const string Component = "Component";

        /// <summary>Infrastructure plus the application(s) as Aspire project resources, tested as a black box.</summary>
        public const string Functional = "Functional";
    }

    /// <summary>Aspire resource names. Lowercase and stable: fixtures look resources up by these names.</summary>
    public static class Resources
    {
        public const string Postgres = "postgres";
        public const string OrdersDb = "ordersdb";
        public const string Kafka = "kafka";

        /// <summary>Third-party HTTP APIs. Its stubs and request log are reset before every test.</summary>
        public const string WireMock = "wiremock";

        /// <summary>OIDC discovery document + JWKS for functional tests. Never reset during a run.</summary>
        public const string Identity = "identity";

        /// <summary>The application under test (functional mode only).</summary>
        public const string OrdersApi = "orders-api";
    }

    /// <summary>
    /// Kafka topics the fixture creates before the app starts (only those the app does not create itself).
    /// Adding a topic here is an allowed additive change in incremental mode.
    /// </summary>
    public static class Topics
    {
        public const string Orders = "orders";
        public const string Payments = "payments";

        public static readonly string[] CreatedByFixture = [Orders, Payments];
    }

    /// <summary>
    /// Configuration keys exactly as the application reads them, ":"-separated. The AppHost turns them into
    /// environment variables ("__"); the component fixture hands them to the WebApplicationFactory.
    /// </summary>
    public static class AppSettings
    {
        /// <summary>Connection string NAME (read as ConnectionStrings:OrdersDb / GetConnectionString("OrdersDb")).</summary>
        public const string OrdersDbConnectionName = "OrdersDb";

        public const string KafkaBootstrapServers = "Kafka:BootstrapServers";
        public const string PaymentsApiBaseUrl = "PaymentsApi:BaseUrl";
        public const string JwtAuthority = "Authentication:Schemes:Bearer:Authority";
        public const string JwtRequireHttpsMetadata = "Authentication:Schemes:Bearer:RequireHttpsMetadata";
    }

    public static string ToEnvironmentVariable(string configurationKey) => configurationKey.Replace(":", "__");
}
