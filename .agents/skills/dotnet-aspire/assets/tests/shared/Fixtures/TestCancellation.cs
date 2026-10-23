using Xunit;

namespace {{SharedNamespace}}.Fixtures;

/// <summary>
/// Token of the running test: cancelled when the run is aborted or the test times out.
/// Shared helpers and test repositories use it internally so test code does not have to pass it, while test methods
/// still pass <c>CancellationToken</c> to HttpClient and other framework APIs (xUnit analyzer xUnit1051).
/// </summary>
public static class TestCancellation
{
    // xUnit v2 has no TestContext: return CancellationToken.None there.
    public static CancellationToken Token => TestContext.Current.CancellationToken;
}
