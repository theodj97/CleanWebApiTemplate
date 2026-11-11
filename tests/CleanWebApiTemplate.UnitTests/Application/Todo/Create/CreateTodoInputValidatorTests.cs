using CleanWebApiTemplate.Application.Todo.Create;

namespace CleanWebApiTemplate.UnitTests.Application.Todo.Create;

/// <summary>Unit coverage of the CreateTodo input rules. Uniqueness is checked by the service, not the validator.</summary>
public class CreateTodoInputValidatorTests
{
    private readonly CreateTodoInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidInput_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "Write the tests", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithWhitespaceTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "   ", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithNullTitle_FailsWithoutThrowing()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = null!, Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithOverlongTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = new string('a', 256), Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "Valid title", Owner = "not-an-email" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
