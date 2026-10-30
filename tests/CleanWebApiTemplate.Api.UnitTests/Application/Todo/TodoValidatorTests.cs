using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Constants;
using FluentValidation;
using FluentValidation.Results;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Todo;

/// <summary>
/// Rules shared by every Todo validator, exercised through a validator built on top of
/// <see cref="TodoValidator{TRequest}"/>. Uniqueness is not a validator rule: the services own it.
/// </summary>
public class TodoValidatorTests
{
    private readonly ExposedTodoValidator _validator = new();

    [Fact]
    public async Task ValidateTitle_WithOverlongTitle_AddsMaxLengthFailure()
    {
        // Arrange
        var title = new string('a', TodoTable.TitleMaxLength + 1);

        // Act
        var result = await ValidateAsync(new TestRequest { Title = title });

        // Assert
        Assert.Contains($"max length is {TodoTable.TitleMaxLength}", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateTitle_WithWhitespaceTitle_AddsRequiredFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Title = "   " });

        // Assert
        Assert.Contains("can't be null or empty", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateTitle_WithValidTitle_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Title = "Unique" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateDescription_WithOverlongDescription_AddsMaxLengthFailure()
    {
        // Arrange
        var description = new string('a', TodoTable.DescriptionMaxLength + 1);

        // Act
        var result = await ValidateAsync(new TestRequest { Description = description });

        // Assert
        Assert.Contains($"max length is {TodoTable.DescriptionMaxLength}", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateDescription_WithValidDescription_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Description = "short description" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("not-an-email", false)]
    [InlineData("user@example.com", true)]
    public async Task ValidateUserEmail_UsesTheEmailRule(string email, bool isValid)
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Email = email });

        // Assert
        Assert.Equal(isValid, result.IsValid);
    }

    [Theory]
    [InlineData((int)ETodoStatus.InProgress, true)]
    [InlineData(99, false)]
    public async Task ValidateStatus_OnlyAcceptsRegisteredValues(int status, bool isValid)
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Status = status });

        // Assert
        Assert.Equal(isValid, result.IsValid);
    }

    private async Task<ValidationResult> ValidateAsync(TestRequest request) =>
        await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

    private sealed class TestRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }
        public int? Status { get; set; }
    }

    private sealed class ExposedTodoValidator : TodoValidator<TestRequest>
    {
        public ExposedTodoValidator()
        {
            RuleFor(x => x.Title)
                .Custom((title, context) => ValidateTitleLength(title!, context))
                .When(x => x.Title is not null);

            RuleFor(x => x.Description)
                .Custom((description, context) => ValidateDescription(description!, context))
                .When(x => x.Description is not null);

            RuleFor(x => x.Email)
                .Custom((email, context) => ValidateUserEmail(email!, context))
                .When(x => x.Email is not null);

            RuleFor(x => x.Status)
                .Custom((status, context) => ValidateStatus(status!.Value, context))
                .When(x => x.Status is not null);
        }
    }
}
