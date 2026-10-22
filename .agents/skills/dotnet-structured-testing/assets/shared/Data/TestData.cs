using Bogus;

namespace {{SharedNamespace}}.Data;

/// <summary>
/// Deterministic test data. Bogus always runs with a fixed seed, so a failing test reproduces with the same data.
/// In serial projects (component, functional) the fixture calls <see cref="BeginTest"/> before every test: the seed is
/// derived from the test's display name, so a test gets the same data whatever order the tests run in.
/// Unit tests do not use this class (they do not reference Shared): they seed their own Faker with a constant.
/// </summary>
public static class TestData
{
    public const int DefaultSeed = 20240611;

    private static int _testSeed = DefaultSeed;
    private static int _fakersCreated;
    private static long _sequence;

    public static void BeginTest(string testName)
    {
        _testSeed = StableHash(testName);
        _fakersCreated = 0;
    }

    /// <summary>Seed for the next generator of the current test (use with <c>new Faker&lt;T&gt;().UseSeed(...)</c>).</summary>
    public static int NextSeed() => unchecked(_testSeed + Interlocked.Increment(ref _fakersCreated));

    /// <summary>A seeded Faker for the current test. Create one per builder instance.</summary>
    public static Faker CreateFaker() => CreateFaker(NextSeed());

    public static Faker CreateFaker(int seed) => new() { Random = new Randomizer(seed) };

    /// <summary>
    /// Unique, readable value for columns with unique constraints (emails, codes): a fixed seed alone would repeat
    /// values when a test creates several entities of the same kind.
    /// </summary>
    public static string Unique(string prefix) => $"{prefix}-{Interlocked.Increment(ref _sequence)}";

    // FNV-1a. string.GetHashCode() is randomized per process and would break reproducibility.
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in value)
            {
                hash = (hash ^ character) * 16777619u;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
