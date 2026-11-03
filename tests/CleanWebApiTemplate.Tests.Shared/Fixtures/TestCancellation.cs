using Xunit;

namespace CleanWebApiTemplate.Tests.Shared.Fixtures;

/// <summary>
/// Token of the running test: cancelled when the run is aborted or the test times out.
/// Shared helpers and test repositories use it internally so test code does not have to pass it.
/// </summary>
public static class TestCancellation
{
    public static CancellationToken Token => TestContext.Current.CancellationToken;
}
