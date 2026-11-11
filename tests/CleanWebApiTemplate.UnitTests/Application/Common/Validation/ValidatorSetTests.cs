using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Application.Todo.Create;

namespace CleanWebApiTemplate.UnitTests.Application.Common.Validation;

/// <summary>An input with no registered validator is a composition error, not a valid input.</summary>
public class ValidatorSetTests
{
    [Fact]
    public void Constructor_WithoutValidators_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new ValidatorSet<CreateTodoInput>([]));

        Assert.Contains(nameof(CreateTodoInput), exception.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithAValidInput_ReturnsNull()
    {
        // Arrange
        var validators = new ValidatorSet<CreateTodoInput>([new CreateTodoInputValidator()]);

        // Act
        var error = await validators.ValidateAsync(
            new CreateTodoInput { Title = "A title", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateAsync_WithAnInvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var validators = new ValidatorSet<CreateTodoInput>([new CreateTodoInputValidator()]);

        // Act
        var error = await validators.ValidateAsync(
            new CreateTodoInput { Title = " ", Owner = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<BadRequestError>(error);
    }
}
