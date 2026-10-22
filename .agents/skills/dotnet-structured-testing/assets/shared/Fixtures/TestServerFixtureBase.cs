using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;
using {{AppNamespace}};
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{SharedNamespace}}.Database;
using {{SharedNamespace}}.Http;
using {{SharedNamespace}}.Messaging;
using {{SharedNamespace}}.Waiting;
using static {{SharedNamespace}}.TestHostContract;

namespace {{SharedNamespace}}.Fixtures;

/// <summary>
/// Environment shared by every test of one test project (registered as an xUnit assembly fixture): Aspire starts the
/// containers once, the schema is prepared once, and tests only reset data between them.
/// Component and functional fixtures derive from it and only add how the application itself is started.
/// </summary>
/// <remarks>
/// Startup order matters and is deliberate:
/// 1. infrastructure containers, all healthy before anything else (otherwise the first tests fail at random);
/// 2. test-side data services, Kafka topics and WireMock client (the app may consume topics as soon as it starts);
/// 3. schema, applied with the production mechanism, BEFORE the app starts — unless the app applies it itself;
/// 4. the application (in-process factory or Aspire project resource);
/// 5. database resetters, which read the final schema (when the app applies it, after waiting until it is complete);
/// 6. test repositories, surviving data (test users...) and readiness checks (e.g. the app's consumer groups own
///    their partitions).
/// </remarks>
public abstract class TestServerFixtureBase : IAsyncLifetime
{
    private ServiceProvider? _dataServices;

    /// <summary>The first run pulls container images, which can take minutes on a slow connection.</summary>
    protected virtual TimeSpan StartupTimeout => TimeSpan.FromMinutes(5);

    /// <summary>Value passed to the AppHost as --TestMode.</summary>
    protected abstract string Mode { get; }

    /// <summary>Generated AppHost project type, e.g. typeof(Projects.Orders_Api_Tests_AppHost).</summary>
    protected abstract Type AppHostEntryPoint { get; }

    /// <summary>ADAPT: true when the app migrates/creates its schema on startup (discovery section 3).</summary>
    protected virtual bool SchemaAppliedByApplication => false;

    /// <summary>Infrastructure resources awaited before anything else. ADAPT: every resource of the AppHost except the apps.</summary>
    protected virtual IReadOnlyList<string> InfrastructureResources =>
        [Resources.Postgres, Resources.OrdersDb, Resources.Kafka, Resources.WireMock];

    public DistributedApplication App { get; private set; } = null!;

    public TestTokenIssuer TokenIssuer { get; } = new(TestAuthSettings.Issuer, TestAuthSettings.Audience);

    public DatabaseResetters Databases { get; } = new();

    public KafkaTestHelper Kafka { get; private set; } = null!;

    public WireMockTestHelper WireMock { get; private set; } = null!;

    // ADAPT: one test repository per aggregate/table the tests arrange or assert on.
    public CustomerTestRepository Customers { get; private set; } = null!;

    public OrderTestRepository Orders { get; private set; } = null!;

    /// <summary>Test-side services (DbContext...) used for the schema and by test repositories.</summary>
    protected IServiceProvider DataServices => _dataServices ?? throw new InvalidOperationException("Fixture not initialized.");

    public async ValueTask InitializeAsync()
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        startup.CancelAfter(StartupTimeout);
        var cancellationToken = startup.Token;

        // 1. Infrastructure. The dashboard is disabled explicitly: no dashboard and no telemetry in tests.
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            AppHostEntryPoint,
            [$"--{ModeKey}={Mode}"],
            (applicationOptions, _) => applicationOptions.DisableDashboard = true,
            cancellationToken);
        App = await builder.BuildAsync(cancellationToken);
        await App.StartAsync(cancellationToken);

        foreach (var resource in InfrastructureResources)
        {
            await App.ResourceNotifications.WaitForResourceHealthyAsync(resource, cancellationToken);
        }

        // 2. Test-side services and helpers.
        _dataServices = await BuildDataServicesAsync(cancellationToken);

        Kafka = new KafkaTestHelper(await RequiredConnectionStringAsync(Resources.Kafka, cancellationToken));
        // ADAPT: partition count production uses. Topics are listed in TestHostContract.Topics.
        await Kafka.CreateTopicsAsync(Topics.CreatedByFixture);

        WireMock = new WireMockTestHelper(App.CreateWireMockAdminClient(Resources.WireMock));

        // 3. Schema before the app starts, so hosted services never see a missing table.
        if (!SchemaAppliedByApplication)
        {
            await ApplySchemaAsync(cancellationToken);
        }

        // 4. The application under test.
        await StartApplicationAsync(cancellationToken);

        // 5. Resetters read the schema, so it must be complete: "started" does not mean "migrated" when the app
        //    migrates in a background service or before its health endpoint answers.
        if (SchemaAppliedByApplication)
        {
            await WaitUntilSchemaIsCompleteAsync(cancellationToken);
        }

        await RegisterDatabaseResettersAsync(cancellationToken);

        // 6. Test repositories, surviving data (also needed by a first test without [ResetDatabase]) and readiness.
        var scopes = DataServices.GetRequiredService<IServiceScopeFactory>();
        Customers = new CustomerTestRepository(scopes);
        Orders = new OrderTestRepository(scopes);

        await SeedPersistentDataAsync(cancellationToken);
        await WaitUntilApplicationIsReadyAsync(cancellationToken);
    }

    /// <summary>Starts the app: in-process factory (component) or Aspire project resource (functional).</summary>
    protected abstract Task StartApplicationAsync(CancellationToken cancellationToken);

    /// <summary>New client for the application under test (anonymous; TestBase adds the identity).</summary>
    public abstract HttpClient CreateHttpClient();

    /// <summary>
    /// Called by TestBase before every test, whatever its attributes: removes state a previous test could leave in
    /// shared infrastructure.
    /// </summary>
    public virtual async Task PrepareForTestAsync(string testName, CancellationToken cancellationToken)
    {
        TestData.BeginTest(testName);
        await WireMock.ResetAsync();
        // Only messages produced from now on are visible to Kafka.WaitForMessageAsync in this test. If the Arrange
        // section itself makes the app publish, the test calls Fixture.Kafka.CaptureOffsetsAsync() again right before Act.
        await Kafka.CaptureOffsetsAsync();
        // ADAPT: if the app consumes messages, wait until it has processed the previous test's messages so they
        // cannot land in this test's data: await Kafka.WaitUntilConsumedAsync("orders-service");
        // ADAPT: purge MailPit, reset the fake clock, clear app in-memory caches... (references/fixtures.md).
    }

    /// <summary>
    /// Inserts data that must always exist (e.g. test users the app looks up by "sub"). Called once at startup and
    /// after every reset. It must be IDEMPOTENT (insert-if-missing / upsert): a reset of another database, or an
    /// ExcludeTables that keeps this table, leaves the rows in place.
    /// ADAPT: upsert TestUsers.All through a test repository when discovery says users must exist in the database.
    /// </summary>
    public virtual Task SeedPersistentDataAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// ADAPT: register the app's DbContext exactly as production does (same provider options, naming conventions,
    /// migrations assembly, history table). Prefer calling the app's own registration extension when it exists,
    /// e.g. services.AddPersistence(configuration).
    /// </summary>
    protected virtual async Task<ServiceProvider> BuildDataServicesAsync(CancellationToken cancellationToken)
    {
        var ordersConnectionString = await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken);

        var services = new ServiceCollection();
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(ordersConnectionString));
        return services.BuildServiceProvider();
    }

    /// <summary>ADAPT: same mechanism as production (EF migrations here; see references/database.md for the others).</summary>
    protected virtual async Task ApplySchemaAsync(CancellationToken cancellationToken)
    {
        await using var scope = DataServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(cancellationToken);
    }

    /// <summary>ADAPT: the app's own mechanism (EF migrations here). Polls until no migration is pending.</summary>
    protected virtual Task WaitUntilSchemaIsCompleteAsync(CancellationToken cancellationToken) =>
        Eventually.AssertAsync(async () =>
        {
            await using var scope = DataServices.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database;
            var pending = await database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                throw new InvalidOperationException($"Pending migrations: {string.Join(", ", pending)}.");
            }
        }, TimeSpan.FromMinutes(2));

    /// <summary>ADAPT: one resetter per database, configured for its engine.</summary>
    protected virtual async Task RegisterDatabaseResettersAsync(CancellationToken cancellationToken)
    {
        var ordersConnectionString = await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken);

        Databases.Add(await RespawnDatabaseResetter.CreateAsync(
            DatabaseResetConfiguration.Names.Orders,
            () => new NpgsqlConnection(ordersConnectionString),
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = DatabaseResetConfiguration.TablesToIgnore(DatabaseResetConfiguration.Names.Orders),
                WithReseed = true,
            },
            cancellationToken));
    }

    /// <summary>ADAPT: e.g. await Kafka.WaitForConsumerGroupAsync("orders-service") when the app consumes topics.</summary>
    protected virtual Task WaitUntilApplicationIsReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected async Task<string> RequiredConnectionStringAsync(string resourceName, CancellationToken cancellationToken) =>
        await App.GetConnectionStringAsync(resourceName, cancellationToken)
        ?? throw new InvalidOperationException($"Resource '{resourceName}' exposes no connection string.");

    public virtual async ValueTask DisposeAsync()
    {
        if (Kafka is not null)
        {
            await Kafka.DisposeAsync();
        }

        if (_dataServices is not null)
        {
            await _dataServices.DisposeAsync();
        }

        if (App is not null)
        {
            // Stops and removes the containers: they live exactly as long as the test session.
            await App.DisposeAsync();
        }

        TokenIssuer.Dispose();
        GC.SuppressFinalize(this);
    }
}
