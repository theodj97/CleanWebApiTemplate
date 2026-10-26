using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Constants;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Todo;

/// <summary>
/// Rules shared by every Todo validator, exercised through a validator built on top of
/// <see cref="TodoValidator{TRequest}"/> with the repository as a substitute: no Docker and no database.
/// </summary>
public class TodoValidatorTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly ExposedTodoValidator _validator;

    public TodoValidatorTests()
    {
        _repository.TitleExistsAsync(Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _validator = new ExposedTodoValidator(_repository);
    }

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
    public async Task ValidateTitle_WithDuplicateTitle_AddsUniquenessFailure()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", Arg.Any<Ulid?>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await ValidateAsync(new TestRequest { Title = "Taken" });

        // Assert
        Assert.Contains("already in use", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateTitle_WithUniqueTitle_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Title = "Unique" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateIdAndTitle_WithoutId_AddsRequiredFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest
        {
            IdAndTitle = new IdAndTitle(null, "Title"),
        });

        // Assert
        Assert.Contains("must be provided.", Assert.Single(result.Errors).ErrorMessage);
        await _repository.Received(1).TitleExistsAsync("Title", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateIdAndTitle_WithOverlongTitle_AddsMaxLengthFailure()
    {
        // Arrange
        var title = new string('a', TodoTable.TitleMaxLength + 1);

        // Act
        var result = await ValidateAsync(new TestRequest
        {
            IdAndTitle = new IdAndTitle(Ulid.NewUlid().ToString(), title),
        });

        // Assert
        Assert.Contains($"max length is {TodoTable.TitleMaxLength}", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateIdAndTitle_WithDuplicateTitle_ExcludesTheCurrentId()
    {
        // Arrange
        var id = Ulid.NewUlid();
        _repository.TitleExistsAsync("Taken", Arg.Any<Ulid?>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await ValidateAsync(new TestRequest
        {
            IdAndTitle = new IdAndTitle(id.ToString(), "Taken"),
        });

        // Assert
        Assert.Contains("already in use", Assert.Single(result.Errors).ErrorMessage);
        await _repository.Received(1).TitleExistsAsync("Taken", id, Arg.Any<CancellationToken>());
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
        public IdAndTitle? IdAndTitle { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }
        public int? Status { get; set; }
    }

    private sealed class ExposedTodoValidator : TodoValidator<TestRequest>
    {
        public ExposedTodoValidator(ITodoRepository repository) : base(repository)
        {
            RuleFor(x => x.Title)
                .CustomAsync((title, context, cancellationToken) => ValidateTitle(title!, context, cancellationToken))
                .When(x => x.Title is not null);

            RuleFor(x => x.IdAndTitle)
                .CustomAsync((idAndTitle, context, cancellationToken) => ValidateTitle(idAndTitle!, context, cancellationToken))
                .When(x => x.IdAndTitle is not null);

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
