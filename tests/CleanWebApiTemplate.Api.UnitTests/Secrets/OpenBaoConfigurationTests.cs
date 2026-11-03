using System.Net;
using System.Text;
using CleanWebApiTemplate.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;

namespace CleanWebApiTemplate.Api.UnitTests.Secrets;

/// <summary>
/// The OpenBao source is opt-in: without an address it changes nothing, and a half-configured store fails loudly
/// instead of starting the application with a missing secret.
/// </summary>
public class OpenBaoConfigurationTests
{
    [Fact]
    public void AddOpenBaoSecrets_WithoutAddress_IsANoOp()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var settings = Settings(("OpenBao:Token", "token"));

        // Act
        var result = builder.AddOpenBaoSecrets(settings);

        // Assert
        Assert.Same(builder, result);
        Assert.Empty(builder.Sources);
    }

    [Theory]
    [InlineData("OpenBao:Token")]
    [InlineData("OpenBao:Path")]
    public void AddOpenBaoSecrets_WithAddressAndAMissingSetting_Throws(string missing)
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var values = new Dictionary<string, string?>
        {
            ["OpenBao:Address"] = "http://localhost:8200",
            ["OpenBao:Token"] = "token",
            ["OpenBao:Path"] = "cleanwebapitemplate/local",
        };
        values.Remove(missing);
        var settings = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddOpenBaoSecrets(settings));

        // Assert
        Assert.Contains(missing, exception.Message);
    }

    [Fact]
    public void AddOpenBaoSecrets_WithACompleteConfiguration_AddsOneSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var settings = Settings(
            ("OpenBao:Address", "http://localhost:8200"),
            ("OpenBao:Token", "token"),
            ("OpenBao:Path", "cleanwebapitemplate/local"));

        // Act
        builder.AddOpenBaoSecrets(settings);

        // Assert
        Assert.Single(builder.Sources);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void AddOpenBaoSecrets_WithAnInvalidTimeout_Throws(string timeout)
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var settings = Settings(
            ("OpenBao:Address", "http://localhost:8200"),
            ("OpenBao:Token", "token"),
            ("OpenBao:Path", "cleanwebapitemplate/local"),
            (OpenBaoConfigurationKeys.TimeoutSeconds, timeout));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddOpenBaoSecrets(settings));

        // Assert
        Assert.Contains(OpenBaoConfigurationKeys.TimeoutSeconds, exception.Message);
    }

    [Fact]
    public void Load_ExposesEveryEntryAsAConfigurationKey()
    {
        // Arrange
        using var http = new HttpClient(new FakeHttpMessageHandler(_ => Json(
            """{"data":{"data":{"API_KEY":"value","Other:Key":"other"}}}""")));
        var provider = new OpenBaoConfigurationProvider(new OpenBaoClient(http, ClientOptions()), "cleanwebapitemplate/local");

        // Act
        provider.Load();

        // Assert
        Assert.True(provider.TryGet("API_KEY", out var apiKey));
        Assert.Equal("value", apiKey);
        Assert.True(provider.TryGet("Other:Key", out var other));
        Assert.Equal("other", other);
        Assert.True(provider.TryGet("other:key", out var caseInsensitive));
        Assert.Equal("other", caseInsensitive);
    }

    [Fact]
    public void Load_WhenTheServerFails_Throws()
    {
        // Arrange
        using var http = new HttpClient(new FakeHttpMessageHandler(_ =>
            Json("""{"errors":["permission denied"]}""", HttpStatusCode.Forbidden)));
        var provider = new OpenBaoConfigurationProvider(new OpenBaoClient(http, ClientOptions()), "cleanwebapitemplate/local");

        // Act & Assert
        Assert.Throws<OpenBaoException>(() => provider.Load());
    }

    private static OpenBaoOptions ClientOptions() => new()
    {
        Address = "http://localhost:8200",
        Token = "root-token",
    };

    private static IConfigurationRoot Settings(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
