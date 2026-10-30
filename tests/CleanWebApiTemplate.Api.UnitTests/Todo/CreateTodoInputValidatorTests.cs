using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Todo;

/// <summary>Unit coverage of the CreateTodo input rules. The repository is a substitute: no Docker, no Aspire.</summary>
public class CreateTodoInputValidatorTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly CreateTodoInputValidator _validator;

    public CreateTodoInputValidatorTests()
    {
        _repository.TitleExistsAsync(Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _validator = new CreateTodoInputValidator(_repository);
    }

    [Fact]
    public async Task Validate_WithValidInput_Passes()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "Write the tests", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyTitle_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithNullTitle_FailsWithoutThrowing()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = null!, CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_Fails()
    {
        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "Valid title", CreatedBy = "not-an-email" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithDuplicateTitle_Fails()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(true);

        // Act
        var result = await _validator.ValidateAsync(
            new CreateTodoInput { Title = "Taken", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
    }
}
