using CleanWebApiTemplate.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Abstractions;

/// <summary>
/// Coverage of the shared validation helpers. Each test exercises one rule of a validator built on top of
/// <see cref="BaseAbstractValidator{TRequest}"/>, so the failure messages are the ones the API really returns.
/// </summary>
public class BaseAbstractValidatorTests
{
    private readonly ExposedValidator _validator = new();

    [Fact]
    public async Task NotNullNotEmpty_WithEmptyValue_AddsFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Value = string.Empty });

        // Assert
        var failure = Assert.Single(result.Errors);
        Assert.StartsWith("Error while validanting property: 'value' .", failure.ErrorMessage);
        Assert.Contains("can't be null or empty", failure.ErrorMessage);
    }

    [Fact]
    public async Task NotNullNotEmpty_WithValue_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Value = "value" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUlid_WithMalformedValue_AddsParseAndLengthFailures()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Id = "not-a-ulid" });

        // Assert
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, failure => failure.ErrorMessage.Contains("valid ULID"));
        Assert.Contains(result.Errors, failure => failure.ErrorMessage.Contains("26 characters"));
    }

    [Fact]
    public async Task ValidateUlid_WithValidValue_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Id = Ulid.NewUlid().ToString() });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateDateTime_WithInvalidValue_AddsFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Date = "not-a-date" });

        // Assert
        Assert.Contains("valid date time", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateStartDateAndEndDate_WithOnlyEndDate_RequiresStartDate()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { EndDate = "2024-01-31" });

        // Assert
        Assert.Contains("StartDate can't be null or empty", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateStartDateAndEndDate_WithOnlyStartDate_RequiresEndDate()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { StartDate = "2024-01-01" });

        // Assert
        Assert.Contains("EndDate can't be null or empty", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateStartDateAndEndDate_WithStartAfterEnd_AddsFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { StartDate = "2024-02-01", EndDate = "2024-01-01" });

        // Assert
        Assert.Contains("must be earlier than", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateStartDateAndEndDate_WithValidRange_DoesNotAddFailure()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { StartDate = "2024-01-01", EndDate = "2024-01-31" });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateSortBy_WithDuplicatedProperty_AddsFailure()
    {
        // Arrange
        var request = new TestRequest
        {
            SortProperties =
            [
                new KeyValuePair<string, bool>("Title", false),
                new KeyValuePair<string, bool>("Title", true),
            ],
        };

        // Act
        var result = await ValidateAsync(request);

        // Assert
        Assert.Contains("duplicated sorts", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateSortBy_WithUnknownProperty_AddsFailure()
    {
        // Arrange
        var request = new TestRequest
        {
            SortProperties = [new KeyValuePair<string, bool>("Unknown", false)],
        };

        // Act
        var result = await ValidateAsync(request);

        // Assert
        Assert.Contains("'Unknown' is not a valid sort property", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidateSortBy_WithKnownProperty_IsCaseInsensitive()
    {
        // Arrange
        var request = new TestRequest
        {
            SortProperties = [new KeyValuePair<string, bool>("title", true)],
        };

        // Act
        var result = await ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("not-an-email", false)]
    public async Task IsValidEmail_ReturnsWhetherTheAddressCanBeParsed(string email, bool expected)
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Email = email });

        // Assert
        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public async Task AddFailure_WithExplicitPropertyName_IncludesIt()
    {
        // Act
        var result = await ValidateAsync(new TestRequest { Explicit = "set" });

        // Assert
        Assert.Equal("Error while validanting property: 'Title' .\nbroken",
                     Assert.Single(result.Errors).ErrorMessage);
    }

    private async Task<ValidationResult> ValidateAsync(TestRequest request) =>
        await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

    private sealed class TestRequest
    {
        public string? Value { get; set; }
        public string? Id { get; set; }
        public string? Date { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }
        public string? Email { get; set; }
        public string? Explicit { get; set; }
    }

    private sealed class ExposedValidator : BaseAbstractValidator<TestRequest>
    {
        private static readonly HashSet<string> ValidProperties =
            new(StringComparer.OrdinalIgnoreCase) { "Title", "CreatedAt" };

        public ExposedValidator()
        {
            RuleFor(x => x.Value)
                .Custom((value, context) => NotNullNotEmpty(value!, context))
                .When(x => x.Value is not null);

            RuleFor(x => x.Id)
                .Custom((id, context) => ValidateUlid(id!, context))
                .When(x => x.Id is not null);

            RuleFor(x => x.Date)
                .Custom((date, context) => ValidateDateTime(date!, context))
                .When(x => x.Date is not null);

            RuleFor(x => new DateRange(x.StartDate, x.EndDate))
                .Custom(ValidateStartDateAndEndDate)
                .When(x => x.StartDate is not null || x.EndDate is not null);

            RuleFor(x => x.SortProperties)
                .Custom((sortProperties, context) => ValidateSortBy(sortProperties, ValidProperties, context))
                .When(x => x.SortProperties is not null);

            RuleFor(x => x.Email)
                .Must(email => IsValidEmail(email!))
                .WithMessage("invalid email")
                .When(x => x.Email is not null);

            RuleFor(x => x.Explicit)
                .Custom((_, context) => AddFailure(context, "broken", propertyName: "Title"))
                .When(x => x.Explicit is not null);
        }
    }
}
