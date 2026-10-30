using CleanWebApiTemplate.Application.Todo.Update;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Todo;

/// <summary>Unit coverage of the UpdateTodo input rules (ULID shape and status enum).</summary>
public class UpdateTodoInputValidatorTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly UpdateTodoInputValidator _validator;

    public UpdateTodoInputValidatorTests()
    {
        _repository.TitleExistsAsync(Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _validator = new UpdateTodoInputValidator(_repository);
    }

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
