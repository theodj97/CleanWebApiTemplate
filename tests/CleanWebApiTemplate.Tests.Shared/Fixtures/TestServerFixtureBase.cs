using System.Security.Cryptography;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using CleanWebApiTemplate.Tests.Shared.Auth;
using CleanWebApiTemplate.Tests.Shared.Data;
using CleanWebApiTemplate.Tests.Shared.Database;
using Npgsql;
using Respawn;
using Xunit;
using static CleanWebApiTemplate.AppHost.AppHostContract;

namespace CleanWebApiTemplate.Tests.Shared.Fixtures;

/// <summary>
/// Environment shared by every test of one test project (registered as an xUnit assembly fixture): the shared AppHost
/// starts the containers once, in a test mode, and tests only reset data between them.
/// Component and functional fixtures derive from it and only add how the application itself is started.
/// </summary>
/// <remarks>
/// Startup order matters and is deliberate:
/// 1. the shared AppHost in <see cref="Mode"/>, with this run's token-trust material; infrastructure healthy;
/// 2. the schema initializer finished with exit code 0 (the initializer local development uses, without seed);
/// 3. the secrets initializer wrote the application's secrets into OpenBao;
/// 4. the test repository;
/// 5. the application (in-process factory or Aspire project resource);
/// 6. database resetters, which read the final schema;
/// 7. surviving data and readiness checks.
/// </remarks>
public abstract class TestServerFixtureBase : IAsyncLifetime
{
    /// <summary>The first run pulls container images, which can take minutes on a slow connection.</summary>
    protected virtual TimeSpan StartupTimeout => TimeSpan.FromMinutes(5);

    /// <summary>AppHostContract.Modes.Component or Functional.</summary>
    protected abstract string Mode { get; }

    /// <summary>Generated type of the shared AppHost, e.g. <c>typeof(apphost::Projects.CleanWebApiTemplate_AppHost)</c>.</summary>
    protected abstract Type AppHostEntryPoint { get; }

    /// <summary>The application under test: decides the token audience and claim types of <c>Client</c>.</summary>
    public abstract string ApplicationResource { get; }

    /// <summary>Every AppHost resource awaited before anything else, except applications and one-shot jobs.</summary>
    protected virtual IReadOnlyList<string> InfrastructureResources =>
        [Resources.Postgres, Resources.PostgresDb, Resources.OpenBao];

    /// <summary>Extra AppHost configuration for this mode, as "--Key=value" arguments.</summary>
    protected virtual IEnumerable<string> AdditionalAppHostArguments => [];

    /// <summary>One-shot resource that applies the schema (null when the application migrates on startup).</summary>
    protected virtual string? SchemaInitializerResource => Resources.DbInitializer;

    /// <summary>One-shot resource that writes the application's secrets into OpenBao.</summary>
    protected virtual string? SecretsInitializerResource => Resources.OpenBaoInitializer;

    /// <summary>True when the application applies its own schema on startup.</summary>
    protected virtual bool SchemaAppliedByApplication => false;

    public DistributedApplication App { get; private set; } = null!;

    /// <summary>
    /// Root token of this run's OpenBao: generated here and handed to the AppHost, so the component tests can point the
    /// in-process application at the same server. Never persisted, never reused between runs.
    /// </summary>
    public string OpenBaoToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>New signing key per run. The AppHost receives only what the applications need to validate.</summary>
    public TestTokenIssuer TokenIssuer { get; } = new(TestAuthSettings.Issuer, TestAuthSettings.Algorithm);

    public DatabaseResetters Databases { get; } = new();

    /// <summary>Repository the tests use to arrange and assert Todo rows.</summary>
    public TodoTestRepository Todos { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        startup.CancelAfter(StartupTimeout);
        var cancellationToken = startup.Token;

        // 1. The shared AppHost in a test mode. No dashboard and no telemetry in tests.
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(
            AppHostEntryPoint,
            [
                $"--{ModeKey}={Mode}",
                $"--{OpenBao.RootTokenParameterKey}={OpenBaoToken}",
                .. TokenIssuer.AppHostArguments(),
                .. AdditionalAppHostArguments
            ],
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

        // 3. Secrets: the initializer writes them into OpenBao, and the application reads them from there at startup.
        if (SecretsInitializerResource is { } secrets)
        {
            await WaitForSuccessfulCompletionAsync(secrets, cancellationToken);
        }

        // 4. Test-side data access.
        var connectionString = await RequiredConnectionStringAsync(Resources.PostgresDb, cancellationToken);
        Todos = new TodoTestRepository(connectionString);

        // 4. The application under test.
        await StartApplicationAsync(cancellationToken);

        // 5. Resetters read the schema, so it must be complete.
        if (SchemaAppliedByApplication)
        {
            await WaitUntilSchemaIsCompleteAsync(cancellationToken);
        }

        await RegisterDatabaseResettersAsync(connectionString, cancellationToken);

        // 6. Surviving data (none today) and readiness.
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
        return Task.CompletedTask;
    }

    /// <summary>
    /// Idempotent insert of data that must always exist. The API has no users table (tokens carry the identity),
    /// so nothing survives a reset today.
    /// </summary>
    public virtual Task SeedPersistentDataAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>One resetter per database, configured for its engine.</summary>
    protected virtual async Task RegisterDatabaseResettersAsync(string connectionString, CancellationToken cancellationToken)
    {
        Databases.Add(await RespawnDatabaseResetter.CreateAsync(
            DatabaseResetConfiguration.Names.Todo,
            () => new NpgsqlConnection(connectionString),
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = DatabaseResetConfiguration.TablesToIgnore(DatabaseResetConfiguration.Names.Todo),
                WithReseed = true,
            },
            cancellationToken));
    }

    protected virtual Task WaitUntilApplicationIsReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task WaitUntilSchemaIsCompleteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
        if (App is not null)
        {
            // Stops and removes the containers: they live exactly as long as the test session.
            await App.DisposeAsync();
        }

        TokenIssuer.Dispose();
        GC.SuppressFinalize(this);
    }
}
