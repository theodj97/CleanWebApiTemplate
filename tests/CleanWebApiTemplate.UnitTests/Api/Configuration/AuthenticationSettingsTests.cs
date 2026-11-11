using CleanWebApiTemplate.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace CleanWebApiTemplate.UnitTests.Api.Configuration;

/// <summary>The two authentication modes are explicit and cannot be mixed.</summary>
public class AuthenticationSettingsTests
{
    private const string ValidKey = "0123456789abcdef0123456789abcdef"; // gitleaks:allow (clave de test)

    [Fact]
    public void Load_WithoutAuthorityAndMode_UsesSymmetric()
    {
        // Act
        var settings = AuthenticationSettings.Load(
            Configuration((Constants.ApiKey, ValidKey)),
            Settings());

        // Assert
        Assert.Equal(AuthenticationMode.Symmetric, settings.Mode);
        Assert.Null(settings.Authority);
        Assert.Equal(ValidKey, settings.ApiKey);
    }

    [Fact]
    public void Load_WithAuthorityWithoutMode_UsesOidc()
    {
        // Act
        var settings = AuthenticationSettings.Load(
            Configuration((Constants.Authority, "https://login.example.com")),
            Settings());

        // Assert
        Assert.Equal(AuthenticationMode.Oidc, settings.Mode);
        Assert.Equal("https://login.example.com", settings.Authority);
        Assert.Null(settings.ApiKey);
    }

    [Fact]
    public void Load_WithSymmetricModeAndAuthority_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration((Constants.AuthenticationMode, "Symmetric"),
                          (Constants.Authority, "https://login.example.com"),
                          (Constants.ApiKey, ValidKey)),
            Settings()));

        Assert.Contains("must be empty", exception.Message);
    }

    [Fact]
    public void Load_WithShortApiKey_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration((Constants.ApiKey, "short")),
            Settings()));

        Assert.Contains("at least 32 UTF-8 bytes", exception.Message);
    }

    [Fact]
    public void Load_WithMissingApiKey_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration(),
            Settings()));

        Assert.Contains("is required", exception.Message);
    }

    [Theory]
    [InlineData("http://login.example.com")]
    [InlineData("not-a-uri")]
    public void Load_WithInvalidOidcAuthority_Throws(string authority)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration((Constants.AuthenticationMode, "Oidc"),
                          (Constants.Authority, authority)),
            Settings()));

        Assert.Contains("absolute HTTPS URI", exception.Message);
    }

    [Fact]
    public void Load_WithOidcModeAndMissingAuthority_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration((Constants.AuthenticationMode, "Oidc")),
            Settings()));

        Assert.Contains("is required", exception.Message);
    }

    [Fact]
    public void Load_WithInvalidMode_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => AuthenticationSettings.Load(
            Configuration((Constants.AuthenticationMode, "Bearer")),
            Settings()));

        Assert.Contains("must be 'Symmetric' or 'Oidc'", exception.Message);
    }

    [Fact]
    public void Load_WithOidcModeAndApiKey_IgnoresApiKey()
    {
        // Act
        var settings = AuthenticationSettings.Load(
            Configuration((Constants.AuthenticationMode, "Oidc"),
                          (Constants.Authority, "https://login.example.com"),
                          (Constants.ApiKey, ValidKey)),
            Settings());

        // Assert
        Assert.Equal(AuthenticationMode.Oidc, settings.Mode);
        Assert.Null(settings.ApiKey);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();

    private static AppSettings Settings() => new()
    {
        ConnectionStrings = new ConnectionStringsSection { Postgres = "Host=localhost" },
        CorsAllowedOrigins = [],
        CorsAllowCredentials = false,
        OpenApiEnabled = true,
        ValidIssuers = ["https://localhost"],
        Audience = "https://localhost:5001",
    };
}
