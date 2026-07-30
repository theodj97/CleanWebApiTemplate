using System.Diagnostics.CodeAnalysis;

namespace CleanWebApiTemplate.Testing.Common;

/// <summary>
/// Tests in this collection never run in parallel with any other collection.
/// Use it for tests that depend on process-wide mutable state (e.g. environment
/// variables) which the functional-test fixture mutates.
/// </summary>
[ExcludeFromCodeCoverage]
[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
public class NonParallelCollection
{
}
