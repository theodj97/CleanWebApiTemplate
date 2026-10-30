using CleanWebApiTemplate.Api.Configuration;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Configuration;

/// <summary>Every required section of <see cref="AppSettings"/> must be reported before the API starts.</summary>
public class AppSettingsValidatorTests
{
    private readonly AppSettingsValidator _validator = new();

    [Fact]
    public void Validate_WithCompleteSettings_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, Settings());

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Fact]
    public void Validate_WithoutConnectionStrings_Fails()
    {
        // Arrange
        var options = new AppSettings
        {
            ConnectionStrings = null!,
            CorsAllowedOrigins = ["https://localhost:5001"],
            CorsAllowCredentials = false,
            OpenApiEnabled = true,
            ValidIssuers = ["https://localhost"],
            Audience = "https://localhost:5001",
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("ConnectionStrings section is required.", result.Failures!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutPostgresConnectionString_Fails(string postgres)
    {
        // Act
        var result = _validator.Validate(null, Settings(postgres: postgres));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("ConnectionStrings.Postgres is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithoutValidIssuers_Fails()
    {
        // Act
        var result = _validator.Validate(null, Settings(issuers: []));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("ValidIssuers must contain at least one entry.", result.Failures!);
    }

    [Fact]
    public void Validate_WithHttpIssuer_Fails()
    {
        // Act
        var result = _validator.Validate(null, Settings(issuers: ["http://localhost"]));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("absolute HTTPS or URN URI"));
    }

    [Fact]
    public void Validate_WithUrnIssuer_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, Settings(issuers: ["urn:CleanWebApiTemplate:tests"]));

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithEmptyIssuer_Fails()
    {
        // Act
        var result = _validator.Validate(null, Settings(issuers: [""]));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("ValidIssuers cannot contain empty entries.", result.Failures!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithoutAudience_Fails(string audience)
    {
        // Act
        var result = _validator.Validate(null, Settings(audience: audience));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Audience is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithoutCorsOrigins_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, Settings(origins: []));

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithSeveralProblems_ReportsEveryFailure()
    {
        // Arrange
        var options = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Postgres = string.Empty },
            CorsAllowedOrigins = [],
            CorsAllowCredentials = false,
            OpenApiEnabled = true,
            ValidIssuers = [],
            Audience = string.Empty,
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(3, result.Failures!.Count());
    }

    private static AppSettings Settings(string postgres = "Host=localhost",
                                        string[]? origins = null,
                                        string[]? issuers = null,
                                        string audience = "https://localhost:5001") => new()
                                        {
                                            ConnectionStrings = new ConnectionStringsSection { Postgres = postgres },
                                            CorsAllowedOrigins = origins ?? ["https://localhost:5001"],
                                            CorsAllowCredentials = false,
                                            OpenApiEnabled = true,
                                            ValidIssuers = issuers ?? ["https://localhost"],
                                            Audience = audience,
                                        };
}
