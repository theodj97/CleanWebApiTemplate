using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host.Configuration;

namespace CleanWebApiTemplate.Testing.UnitTests.Configuration;

public class AppSettingsValidatorTests
{
    private readonly AppSettingsValidator _validator = new();

    [Fact]
    public void Validate_WithValidSettings_Should_ReturnSuccess()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "Data Source=test.db" },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_WithNullConnectionStrings_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = null!,
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings section is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithEmptySqliteConnection_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = string.Empty },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings.Sqlite is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithWhitespaceSqliteConnection_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "   " },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings.Sqlite is required.", result.Failures!);
    }

    [Fact]
    public void Validate_WithNullCorsAllow_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "Data Source=test.db" },
            CorsAllow = null!,
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("CorsAllow must contain at least one entry.", result.Failures!);
    }

    [Fact]
    public void Validate_WithEmptyCorsAllow_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "Data Source=test.db" },
            CorsAllow = [],
            ValidIssuers = ["localhost"]
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("CorsAllow must contain at least one entry.", result.Failures!);
    }

    [Fact]
    public void Validate_WithNullValidIssuers_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "Data Source=test.db" },
            CorsAllow = ["*"],
            ValidIssuers = null!
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("ValidIssuers must contain at least one entry.", result.Failures!);
    }

    [Fact]
    public void Validate_WithEmptyValidIssuers_Should_Fail()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = new ConnectionStringsSection { Sqlite = "Data Source=test.db" },
            CorsAllow = ["*"],
            ValidIssuers = []
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("ValidIssuers must contain at least one entry.", result.Failures!);
    }

    [Fact]
    public void Validate_WithAllInvalid_Should_ReturnMultipleFailures()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConnectionStrings = null!,
            CorsAllow = [],
            ValidIssuers = []
        };

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        Assert.True(result.Failed);
        Assert.Equal(3, result.Failures!.Count());
    }
}
