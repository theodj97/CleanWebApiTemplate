using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Domain.Configuration;

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
            CorsAllow = ["http://localhost"],
            ValidIssuers = ["http://localhost"],
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
    public void Validate_WithoutCorsAllow_Fails()
    {
        // Act
        var result = _validator.Validate(null, Settings(cors: []));

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("CorsAllow must contain at least one entry.", result.Failures!);
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
    public void Validate_WithSeveralProblems_ReportsEveryFailure()
    {
        // Arrange
        var options = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Postgres = string.Empty },
            CorsAllow = [],
            ValidIssuers = [],
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(3, result.Failures!.Count());
    }

    private static AppSettings Settings(string postgres = "Host=localhost",
                                        string[]? cors = null,
                                        string[]? issuers = null) => new()
                                        {
                                            ConnectionStrings = new ConnectionStringsSection { Postgres = postgres },
                                            CorsAllow = cors ?? ["http://localhost"],
                                            ValidIssuers = issuers ?? ["http://localhost"],
                                        };
}
