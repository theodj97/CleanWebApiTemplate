using System.Net;
using System.Text;
using CleanWebApiTemplate.Infrastructure.Secrets;

namespace CleanWebApiTemplate.Api.UnitTests.Secrets;

/// <summary>The hand-written client only has to speak KV v2 correctly and never leak the token in an error.</summary>
public class OpenBaoClientTests
{
    private static readonly OpenBaoOptions Options = new()
    {
        Address = "http://localhost:8200",
        Token = "root-token",
    };

    [Fact]
    public async Task ReadAsync_ReturnsEverySecretEntry()
    {
        // Arrange
        using var http = new HttpClient(new FakeHttpMessageHandler(_ =>
            Json("""{"data":{"data":{"API_KEY":"value","Other:Key":"other"}}}""")));
        var client = new OpenBaoClient(http, Options);

        // Act
        var values = await client.ReadAsync("cleanwebapitemplate/local", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, values.Count);
        Assert.Equal("value", values["API_KEY"]);
        Assert.Equal("other", values["Other:Key"]);
    }

    [Fact]
    public async Task ReadAsync_SendsTheTokenAndUsesTheKvV2Path()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => Json("""{"data":{"data":{}}}"""));
        using var http = new HttpClient(handler);
        var client = new OpenBaoClient(http, Options);

        // Act
        await client.ReadAsync("/cleanwebapitemplate/local", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            "http://localhost:8200/v1/secret/data/cleanwebapitemplate/local",
            handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("root-token", Assert.Single(handler.LastRequest.Headers.GetValues("X-Vault-Token")));
    }

    [Fact]
    public async Task ReadAsync_WithServerError_ReportsStatusPathAndReasonButNeverTheToken()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            Json("""{"errors":["permission denied"]}""", HttpStatusCode.Forbidden));
        using var http = new HttpClient(handler);
        var client = new OpenBaoClient(http, Options);

        // Act
        var exception = await Assert.ThrowsAsync<OpenBaoException>(
            () => client.ReadAsync("cleanwebapitemplate/local", TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains("403", exception.Message);
        Assert.Contains("cleanwebapitemplate/local", exception.Message);
        Assert.Contains("permission denied", exception.Message);
        Assert.DoesNotContain("root-token", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_WithUnexpectedPayload_Throws()
    {
        // Arrange
        using var http = new HttpClient(new FakeHttpMessageHandler(_ => Json("""{"data":{}}""")));
        var client = new OpenBaoClient(http, Options);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OpenBaoException>(() => client.ReadAsync("app", TestContext.Current.CancellationToken));
        Assert.Contains("data.data", exception.Message);
    }

    [Fact]
    public async Task WriteAsync_PostsTheValuesWrappedInData()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => Json("""{"data":{"version":1}}"""));
        using var http = new HttpClient(handler);
        var client = new OpenBaoClient(http, Options);

        // Act
        await client.WriteAsync("app", new Dictionary<string, string> { ["API_KEY"] = "value" }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:8200/v1/secret/data/app", handler.LastRequest.RequestUri!.ToString());
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"data\":", handler.LastBody);
        Assert.Contains("\"API_KEY\":\"value\"", handler.LastBody);
    }

    [Theory]
    [InlineData("", "root-token")]
    [InlineData("not-a-url", "root-token")]
    [InlineData("ftp://localhost:8200", "root-token")]
    [InlineData("http://localhost:8200", " ")]
    public void Constructor_WithInvalidOptions_Throws(string address, string token)
    {
        // Arrange
        using var http = new HttpClient(new FakeHttpMessageHandler(_ => Json("{}")));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OpenBaoClient(http, new OpenBaoOptions
        {
            Address = address,
            Token = token,
        }));
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
