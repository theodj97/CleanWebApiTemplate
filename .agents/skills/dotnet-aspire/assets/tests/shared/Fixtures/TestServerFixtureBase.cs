using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using {{AppNamespace}};
using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Data;
using {{SharedNamespace}}.Database;
using {{SharedNamespace}}.Waiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;
using static {{AppHostNamespace}}.AppHostContract;

namespace {{SharedNamespace}}.Fixtures;

/// <summary>
/// Environment shared by every test of one test project (registered as an xUnit assembly fixture): the shared AppHost
/// starts the containers once, in a test mode, and tests only reset data between them.
/// Component and functional fixtures derive from it and only add how the application itself is started.
/// </summary>
/// <remarks>
/// Startup order matters and is deliberate:
/// 1. the shared AppHost in <see cref="Mode"/>, with this run's token-trust material; infrastructure healthy;
/// 2. the schema initializer finished with exit code 0 (the same initializer local development uses, without seed);
/// 3. test-side data services and helpers;
/// 4. the application (in-process factory or Aspire project resource);
/// 5. database resetters, which read the final schema;
/// 6. test repositories, surviving data and readiness checks.
/// ADAPT: every "Orders"/PostgreSQL member is an example; commented members are optional patterns (Kafka, third-party
/// HTTP APIs) to enable only when the application uses them. See references/integration-testing.md.
/// </remarks>
public abstract class TestServerFixtureBase : IAsyncLifetime
{
    private ServiceProvider? _dataServices;

    /// <summary>The first run pulls container images, which can take minutes on a slow connection.</summary>
    protected virtual TimeSpan StartupTimeout => TimeSpan.FromMinutes(5);

    /// <summary>AppHostContract.Modes.Component or Functional.</summary>
    protected abstract string Mode { get; }

    /// <summary>Generated type of the shared AppHost, e.g. typeof(apphost::Projects.Contoso_Orders_AppHost). Shared does not
    /// reference the AppHost project: the test projects do.</summary>
    protected abstract Type AppHostEntryPoint { get; }

    /// <summary>The application under test: decides the token audience and claim types of <c>Client</c>.</summary>
    public abstract string ApplicationResource { get; }

    /// <summary>ADAPT: every AppHost resource awaited before anything else, except applications and one-shot jobs.</summary>
    protected virtual IReadOnlyList<string> InfrastructureResources => [Resources.Postgres, Resources.OrdersDb];

    /// <summary>Extra AppHost configuration for this mode, as "--Key=value" arguments.</summary>
    protected virtual IEnumerable<string> AdditionalAppHostArguments => [];

    /// <summary>One-shot resource that applies the schema (null when the application migrates on startup).</summary>
    protected virtual string? SchemaInitializerResource => Resources.DbInitializer;

    /// <summary>ADAPT: true when the application applies its own schema on startup (SchemaInitializerResource is then null).</summary>
    protected virtual bool SchemaAppliedByApplication => false;

    public DistributedApplication App { get; private set; } = null!;

    /// <summary>New signing key per run. The AppHost receives only what the applications need to validate.</summary>
    public TestTokenIssuer TokenIssuer { get; } = new(TestAuthSettings.Issuer, TestAuthSettings.Algorithm);

    public DatabaseResetters Databases { get; } = new();

    // ADAPT (Kafka): public KafkaTestHelper Kafka { get; private set; } = null!;
    // ADAPT (third-party HTTP APIs): public WireMockTestHelper WireMock { get; private set; } = null!;

    // ADAPT: one test repository per aggregate/table the tests arrange or assert on.

    public OrderTestRepository Orders { get; private set; } = null!;

    public UserTestRepository Users { get; private set; } = null!;

    /// <summary>Test-side services (DbContext...) used by resetters and test repositories.</summary>
    protected IServiceProvider DataServices => _dataServices ?? throw new InvalidOperationException("Fixture not initialized.");

    public async ValueTask InitializeAsync()
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        startup.CancelAfter(StartupTimeout);
        var cancellationToken = startup.Token;

        // 1. The shared AppHost in a test mode. No dashboard and no telemetry in tests.
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            AppHostEntryPoint,
            [$"--{ModeKey}={Mode}", .. TokenIssuer.AppHostArguments(), .. AdditionalAppHostArguments],
            (applicationOptions, _) => applicationOptions.DisableDashboard = true,
            cancellationToken);
        App = await builder.BuildAsync(cancellationToken);
        await App.StartAsync(cancellationToken);

        foreach (var resource in InfrastructureResources)
        {
            await App.ResourceNotifications.WaitForResourceHealthyAsync(resource, cancellationToken);
        }

        // 2. Schema, applied by the shared initializer before any application starts.
        if (SchemaInitializerResource is { } initializer)
        {
            await WaitForSuccessfulCompletionAsync(initializer, cancellationToken);
        }

        // 3. Test-side services and helpers.
        _dataServices = await BuildDataServicesAsync(cancellationToken);
        // ADAPT (Kafka): topics before any application consumer subscribes (partition count as in production).
        // Kafka = new KafkaTestHelper(await RequiredConnectionStringAsync(Resources.Kafka, cancellationToken));
        // await Kafka.CreateTopicsAsync(Topics.CreatedByFixture);
        // ADAPT (third-party HTTP APIs):
        // WireMock = new WireMockTestHelper(App.CreateWireMockAdminClient(Resources.WireMock));

        // 4. The application under test.
        await StartApplicationAsync(cancellationToken);

        // 5. Resetters read the schema, so it must be complete: "started" does not mean "migrated" when the
        //    application migrates in a background service or before its health endpoint answers.
        if (SchemaAppliedByApplication)
        {
            await WaitUntilSchemaIsCompleteAsync(cancellationToken);
        }

        await RegisterDatabaseResettersAsync(cancellationToken);

        // 6. Test repositories, surviving data (also needed by a first test without [ResetDatabase]) and readiness.
        var scopes = DataServices.GetRequiredService<IServiceScopeFactory>();
        Orders = new OrderTestRepository(scopes);
        Users = new UserTestRepository(scopes);

        await SeedPersistentDataAsync(cancellationToken);
        await WaitUntilApplicationIsReadyAsync(cancellationToken);
    }

    /// <summary>Starts the app: in-process factory (component) or Aspire project resource (functional).</summary>
    protected abstract Task StartApplicationAsync(CancellationToken cancellationToken);

    /// <summary>New anonymous client for the application under test (TestBase adds the identity).</summary>
    public abstract HttpClient CreateHttpClient();

    /// <summary>Called by TestBase before every test: removes state a previous test could leave in shared infrastructure.</summary>
    public virtual Task PrepareForTestAsync(string testName, CancellationToken cancellationToken)
    {
        TestData.BeginTest(testName);
        // ADAPT: await WireMock.ResetAsync(); await Kafka.CaptureOffsetsAsync(); for consuming apps also
        // await Kafka.WaitUntilConsumedAsync("<group>"); purge MailPit, reset the fake clock, clear in-memory caches
        // (references/integration-testing.md, Per-test reset of shared state).
        return Task.CompletedTask;
    }

    /// <summary>
    /// Idempotent insert of data that must always exist (insert-if-missing / upsert). Called at startup and after every
    /// reset. ADAPT: upsert TestUsers.All when the application looks users up by "sub".
    /// </summary>
    public virtual Task SeedPersistentDataAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>ADAPT: registers the app's DbContext exactly as production does (same provider, options, naming
    /// conventions, migrations assembly); prefer the app's own registration extension when it exists.</summary>
    protected virtual async Task<ServiceProvider> BuildDataServicesAsync(CancellationToken cancellationToken)
    {
        var connectionString = await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken);
        var services = new ServiceCollection();
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        return services.BuildServiceProvider();
    }

    /// <summary>ADAPT: one resetter per database, configured for its engine (references/database.md).</summary>
    protected virtual async Task RegisterDatabaseResettersAsync(CancellationToken cancellationToken)
    {
        var connectionString = await RequiredConnectionStringAsync(Resources.OrdersDb, cancellationToken);
        Databases.Add(await RespawnDatabaseResetter.CreateAsync(
            DatabaseResetConfiguration.Names.Orders,
            () => new NpgsqlConnection(connectionString),
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = DatabaseResetConfiguration.TablesToIgnore(DatabaseResetConfiguration.Names.Orders),
                WithReseed = true,
            },
            cancellationToken));
    }

    /// <summary>ADAPT: e.g. await Kafka.WaitForConsumerGroupAsync("<group>") when the application consumes topics.</summary>
    protected virtual Task WaitUntilApplicationIsReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>ADAPT: the application's own schema mechanism (EF migrations here). Polls until nothing is pending.</summary>
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

    protected async Task<string> RequiredConnectionStringAsync(string resourceName, CancellationToken cancellationToken) =>
        await App.GetConnectionStringAsync(resourceName, cancellationToken)
        ?? throw new InvalidOperationException($"Resource '{resourceName}' exposes no connection string.");

    /// <summary>Waits until a one-shot resource exits and fails the run, with a clear message, unless the exit code is 0.</summary>
    protected async Task WaitForSuccessfulCompletionAsync(string resourceName, CancellationToken cancellationToken)
    {
        var finished = await App.ResourceNotifications.WaitForResourceAsync(
            resourceName,
            resourceEvent => KnownResourceStates.TerminalStates.Contains(resourceEvent.Snapshot.State?.Text),
            cancellationToken);

        if (finished.Snapshot.ExitCode is not 0)
        {
            throw new InvalidOperationException(
                $"'{resourceName}' ended in state '{finished.Snapshot.State?.Text}' with exit code {finished.Snapshot.ExitCode}. " +
                "Its logs are in the test output (ResourceLoggerService) or `aspire logs` when reproduced locally.");
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        // ADAPT (Kafka): if (Kafka is not null) { await Kafka.DisposeAsync(); }
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
