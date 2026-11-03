using System.Net.Http.Headers;
using System.Reflection;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Auth;
using Xunit;
using Xunit.v3;

namespace CleanWebApiTemplate.Tests.Shared.Fixtures;

/// <summary>
/// Base class of every component and functional test class. Before each test it:
/// 1. resets shared infrastructure state (test-data seed);
/// 2. applies <see cref="ResetDatabaseAttribute"/> (method over class) asynchronously, then re-seeds surviving data;
/// 3. builds <see cref="Client"/> for <see cref="AuthenticatedAsAttribute"/> / <see cref="UnauthenticatedAttribute"/>
///    (method over class; anonymous when there is no attribute).
/// The work lives here, not in the attributes, because xUnit attribute hooks are synchronous and blocking on async
/// I/O inside xUnit can deadlock; InitializeAsync is awaited by xUnit and has the current test in TestContext.
/// </summary>
public abstract class TestBase<TFixture>(TFixture fixture) : IAsyncLifetime
    where TFixture : TestServerFixtureBase
{
    private readonly List<HttpClient> _clients = [];

    protected TFixture Fixture { get; } = fixture;

    /// <summary>Client with the identity declared by the attributes (anonymous by default).</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Cancelled when the run is aborted or the test times out. Pass it to every async call that accepts one.</summary>
    protected CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var method = CurrentTestMethod();
        var testClass = GetType();
        var testName = TestContext.Current.Test?.TestDisplayName ?? $"{testClass.FullName}.{method.Name}";

        await Fixture.PrepareForTestAsync(testName, CancellationToken);

        var reset = method.GetCustomAttribute<ResetDatabaseAttribute>()
            ?? testClass.GetCustomAttribute<ResetDatabaseAttribute>();
        if (reset is not null)
        {
            await Fixture.Databases.ResetAsync(reset.Databases, reset.ExcludeTables, CancellationToken);
            await Fixture.SeedPersistentDataAsync(CancellationToken);
        }

        Client = ResolveUser(method, testClass) is { } user ? CreateClient(user) : CreateAnonymousClient();
    }

    /// <summary>
    /// Client for a specific identity. Use it in Arrange when the identity varies per [Theory] row, or when one test
    /// needs several identities (A creates a resource, B tries to read it).
    /// </summary>
    protected HttpClient CreateClient(TestUser user) => Authorize(CreateAnonymousClient(), user);

    protected HttpClient CreateAnonymousClient() => Track(Fixture.CreateHttpClient());

    /// <summary>Adds a locally signed bearer token of <paramref name="user"/>, shaped for the application under test.</summary>
    protected HttpClient Authorize(HttpClient client, TestUser user, string? resourceName = null)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Fixture.TokenIssuer.CreateToken(user, resourceName ?? Fixture.ApplicationResource));
        return client;
    }

    /// <summary>Client that sends a deliberately invalid token (authentication tests: the app must answer 401).</summary>
    protected HttpClient CreateClientWithInvalidToken(InvalidToken kind, TestUser user)
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Fixture.TokenIssuer.CreateInvalidToken(kind, user, Fixture.ApplicationResource));
        return client;
    }

    /// <summary>Registers a client for disposal at the end of the test.</summary>
    protected HttpClient Track(HttpClient client)
    {
        _clients.Add(client);
        return client;
    }

    public virtual ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private static TestUser? ResolveUser(MethodInfo method, Type testClass)
    {
        if (method.IsDefined(typeof(UnauthenticatedAttribute), inherit: true))
        {
            return null;
        }

        if (method.GetCustomAttribute<AuthenticatedAsAttribute>() is { } onMethod)
        {
            return onMethod.User;
        }

        if (testClass.IsDefined(typeof(UnauthenticatedAttribute), inherit: true))
        {
            return null;
        }

        return testClass.GetCustomAttribute<AuthenticatedAsAttribute>()?.User;
    }

    // TestContext.Current is populated during the test class constructor and InitializeAsync (xUnit v3).
    private static MethodInfo CurrentTestMethod() =>
        (TestContext.Current.TestMethod as IXunitTestMethod)?.Method
        ?? throw new InvalidOperationException(
            "TestContext.Current has no xUnit test method. TestBase requires xUnit v3; see references/integration-testing.md (xUnit v2 variant).");
}
