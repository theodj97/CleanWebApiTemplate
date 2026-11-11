using CleanWebApiTemplate.Application.Todo.Update;

namespace CleanWebApiTemplate.UnitTests.Application.Todo.Update;

/// <summary>Unit coverage of the UpdateTodo input rules (ULID shape and status enum).</summary>
public class UpdateTodoInputValidatorTests
{
    private readonly UpdateTodoInputValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidIdAndNoChanges_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput { Id = Ulid.NewUlid().ToString(), LastWriter = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidUlid_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new UpdateTodoInput { Id = "not-a-ulid", LastWriter = "user@example.com" },
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
                LastWriter = "user@example.com",
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
            new UpdateTodoInput { Id = Ulid.NewUlid().ToString(), Status = status, LastWriter = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
