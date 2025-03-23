using System.Diagnostics.CodeAnalysis;

namespace CleanWebApiTemplate.Testing;

[ExcludeFromCodeCoverage]
[CollectionDefinition(nameof(TestServerFixtureCollection))]
public class TestServerFixtureCollection : ICollectionFixture<TestServerFixture>
{
}
