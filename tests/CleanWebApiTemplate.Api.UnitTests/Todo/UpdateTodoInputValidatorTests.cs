using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.Api.UnitTests.Todo;

/// <summary>Unit coverage of the UpdateTodo input rules (ULID shape and status enum).</summary>
public class UpdateTodoInputValidatorTests
{
    private readonly UpdateTodoInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidIdAndNoChanges_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput { Id = Ulid.NewUlid().ToString(), UpdatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidUlid_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput { Id = "not-a-ulid", UpdatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithWhitespaceTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput
            {
                Id = Ulid.NewUlid().ToString(),
                Title = "   ",
                UpdatedBy = "user@example.com",
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithOverlongTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput
            {
                Id = Ulid.NewUlid().ToString(),
                Title = new string('a', 256),
                UpdatedBy = "user@example.com",
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
            new UpdateTodoInput { Id = Ulid.NewUlid().ToString(), Status = status, UpdatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
