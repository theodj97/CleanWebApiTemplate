using {{SharedNamespace}}.Auth;
using {{SharedNamespace}}.Fixtures;

namespace {{FunctionalTestsNamespace}};

/// <summary>
/// Base class of every functional test class (see TestBase for the per-test lifecycle). It deliberately offers no
/// access to the application's services: functional tests interact through HTTP and messages only.
/// </summary>
public abstract class FunctionalTestBase(FunctionalTestFixture fixture) : TestBase<FunctionalTestFixture>(fixture)
{
    /// <summary>Client of <paramref name="user"/> for another application of the flow (e.g. "billing-api").</summary>
    protected HttpClient CreateClient(TestUser user, string resourceName) =>
        Authorize(Track(Fixture.CreateHttpClient(resourceName)), user, resourceName);

    protected HttpClient CreateAnonymousClient(string resourceName) => Track(Fixture.CreateHttpClient(resourceName));
}
