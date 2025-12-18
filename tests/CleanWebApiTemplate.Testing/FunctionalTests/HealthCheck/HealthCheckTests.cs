using System.Net;

namespace CleanWebApiTemplate.Testing.FunctionalTests.HealthCheck;

[Collection(nameof(TestServerFixtureCollection))]
public class HealthCheckTests(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;
    private const string API_HEALTH_URL = "/health/api";

    [Fact]
    public async Task CheckApiHealth_Should_Return_Ok()
    {
        // Act
        var response = await Fixture.HttpClient.GetAsync(API_HEALTH_URL);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
