using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Domain.Project;
using FluentValidation;
using FluentValidation.Results;

namespace CleanWebApiTemplate.UnitTests.Application.Project;

/// <summary>
/// Rules shared by every Project validator, exercised through a validator built on top of
/// <see cref="ProjectValidator{TRequest}"/>. Uniqueness is not a validator rule: the services own it.
/// </summary>
public class ProjectValidatorTests
{
    private readonly ExposedProjectValidator _validator = new();

    [Fact]
    public async Task ValidateName_WithOverlongName_AddsMaxLengthFailure()
    {
        // Arrange
        var name = new string('a', ProjectConstraints.NameMaxLength + 1);

        // Act
        var result = await ValidateAsync(new TestRequest { Name = name });

        // Assert
        Assert.Contains($"max length is {ProjectConstraints.NameMaxLength}", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateName_WithWhitespaceName_AddsRequiredFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Name = "   " });

        // Assert
        Assert.Contains("can't be null or empty", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateName_WithValidName_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Name = "Unique" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateDescription_WithOverlongDescription_AddsMaxLengthFailure()
    {
        // Arrange
        var description = new string('a', ProjectConstraints.DescriptionMaxLength + 1);

        // Act
        var result = await ValidateAsync(new TestRequest { Description = description });

        // Assert
        Assert.Contains($"max length is {ProjectConstraints.DescriptionMaxLength}", Assert.Single(result.Errors).ErrorMessage);
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
    public async Task ValidateOwnerEmail_UsesTheEmailRule(string email, bool isValid)
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Email = email });

        // Assert
        Assert.Equal(isValid, result.IsValid);
    }

    [Theory]
    [InlineData((int)EProjectStatus.Active, true)]
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
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }
        public int? Status { get; set; }
    }

    private sealed class ExposedProjectValidator : ProjectValidator<TestRequest>
    {
        public ExposedProjectValidator()
        {
            RuleFor(x => x.Name)
                .Custom((name, context) => ValidateNameLength(name!, context))
                .When(x => x.Name is not null);

            RuleFor(x => x.Description)
                .Custom((description, context) => ValidateDescription(description!, context))
                .When(x => x.Description is not null);

            RuleFor(x => x.Email)
                .Custom((email, context) => ValidateOwnerEmail(email!, context))
                .When(x => x.Email is not null);

            RuleFor(x => x.Status)
                .Custom((status, context) => ValidateStatus(status!.Value, context))
                .When(x => x.Status is not null);
        }
    }
}
