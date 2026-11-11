using {App}.Application.Project.Handlers.Create;

namespace {App}.Api.UnitTests.Project;

/// <summary>Unit coverage of the CreateProject input rules. Uniqueness is checked by the handler, not the validator.</summary>
public class CreateProjectInputValidatorTests
{
    private readonly CreateProjectInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidInput_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "Release 1.0", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithWhitespaceName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "   ", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithNullName_FailsWithoutThrowing()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = null!, CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithOverlongName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = new string('a', 121), CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateProjectInput { Name = "Valid name", CreatedBy = "not-an-email" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
