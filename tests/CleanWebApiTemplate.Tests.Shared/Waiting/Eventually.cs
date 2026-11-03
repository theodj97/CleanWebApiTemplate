using CleanWebApiTemplate.Tests.Shared.Fixtures;

namespace CleanWebApiTemplate.Tests.Shared.Waiting;

/// <summary>
/// Waits for eventual effects by polling with a timeout. Never synchronise a test with Thread.Sleep or a fixed
/// Task.Delay: they are too short on a slow CI agent and waste time everywhere else.
/// </summary>
public static class Eventually
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Retries the assertion until it passes; on timeout rethrows its last failure, so the test shows why.</summary>
    public static async Task AssertAsync(Func<Task> assertion, TimeSpan? timeout = null)
    {
        var cancellationToken = TestCancellation.Token;
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

        while (true)
        {
            try
            {
                await assertion();
                return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                // Not there yet: poll again.
            }

            await Task.Delay(PollInterval, cancellationToken); // polling interval, not a synchronisation delay
        }
    }

    /// <summary>
    /// Polls <paramref name="probe"/> until it returns a value matching <paramref name="until"/>; default on timeout.
    /// Exceptions from the probe (e.g. "connection refused" while an app starts) count as "not yet".
    /// </summary>
    public static async Task<T?> GetAsync<T>(Func<Task<T?>> probe, Func<T, bool>? until = null, TimeSpan? timeout = null)
    {
        var cancellationToken = TestCancellation.Token;
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var value = await probe();
                if (value is not null && (until is null || until(value)))
                {
                    return value;
                }
            }
            catch (Exception) when (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                // Not available yet: poll again.
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        return default;
    }
}
