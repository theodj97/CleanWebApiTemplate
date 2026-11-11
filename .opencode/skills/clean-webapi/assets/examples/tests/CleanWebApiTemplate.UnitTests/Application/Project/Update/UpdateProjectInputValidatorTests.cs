using CleanWebApiTemplate.Application.Project.Update;

namespace CleanWebApiTemplate.UnitTests.Application.Project.Update;

/// <summary>Unit coverage of the UpdateProject input rules (ULID shape and status enum).</summary>
public class UpdateProjectInputValidatorTests
{
    private readonly UpdateProjectInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidIdAndNoChanges_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateProjectInput { Id = Ulid.NewUlid().ToString(), LastWriter = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidUlid_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateProjectInput { Id = "not-a-ulid", LastWriter = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithWhitespaceName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateProjectInput
            {
                Id = Ulid.NewUlid().ToString(),
                Name = "   ",
                LastWriter = "user@example.com",
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithOverlongName_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateProjectInput
            {
                Id = Ulid.NewUlid().ToString(),
                Name = new string('a', 256),
                LastWriter = "user@example.com",
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task Validate_WithUnregisteredStatus_Fails(int status)
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateProjectInput { Id = Ulid.NewUlid().ToString(), Status = status, LastWriter = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
