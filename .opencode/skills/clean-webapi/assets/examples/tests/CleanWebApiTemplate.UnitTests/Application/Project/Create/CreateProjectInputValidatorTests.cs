using CleanWebApiTemplate.Application.Project.Create;

namespace CleanWebApiTemplate.UnitTests.Application.Project.Create;

/// <summary>Unit coverage of the CreateProject input rules. Uniqueness is checked by the service, not the validator.</summary>
public class CreateProjectInputValidatorTests
{
    private readonly CreateProjectInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidInput_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "Write the tests", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithWhitespaceName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "   ", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithNullName_FailsWithoutThrowing()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = null!, Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithOverlongName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = new string('a', 256), Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "Valid name", Owner = "not-an-email" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
