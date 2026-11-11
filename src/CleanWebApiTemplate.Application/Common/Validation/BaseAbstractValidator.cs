using System.Globalization;
using System.Net.Mail;
using System.Text;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Common.Validation;

/// <summary>
/// Base for the application's validators: shared rules for ids, dates, emails and sort keys, plus a
/// consistent failure format. Property rules cascade, so one request reports every broken rule at once.
/// </summary>
public abstract class BaseAbstractValidator<TRequest> : AbstractValidator<TRequest> where TRequest : class
{
    protected BaseAbstractValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
    }

    /// <summary>Fails when the property is null, empty or whitespace.</summary>
    protected void NotNullNotEmpty(string property, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrWhiteSpace(property))
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
    }

    /// <summary>Fails unless the value is a 26-character ULID.</summary>
    protected void ValidateUlid(string id, ValidationContext<TRequest> context)
    {
        if (Ulid.TryParse(id, out _) is false || id.Length is not 26)
        {
            AddFailure(context, $"Property '{context.DisplayName}' must be a valid 26-character ULID");
        }
    }

    /// <summary>Fails unless the value parses as a date with the invariant culture and a round-trip kind.</summary>
    protected void ValidateDateTime(string dateTime, ValidationContext<TRequest> context)
    {
        if (DateTime.TryParse(dateTime,
                              CultureInfo.InvariantCulture,
                              DateTimeStyles.RoundtripKind,
                              out _) is false)
        {
            AddFailure(context, $"Property '{context.DisplayName}' must be a valid date time");
        }
    }

    /// <summary>Fails when only one bound is present or when the range is reversed.</summary>
    protected void ValidateStartDateAndEndDate(DateRange dateRange, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrEmpty(dateRange.StartDate)
            && string.IsNullOrEmpty(dateRange.EndDate) is false)
        {
            AddFailure(context, $"{nameof(DateRange.StartDate)} can't be null or empty when {nameof(DateRange.EndDate)} has value");
        }

        if (string.IsNullOrEmpty(dateRange.EndDate)
            && string.IsNullOrEmpty(dateRange.StartDate) is false)
        {
            AddFailure(context, $"{nameof(DateRange.EndDate)} can't be null or empty when {nameof(DateRange.StartDate)} has value");
        }

        bool isStartDateValid = DateTime.TryParse(dateRange.StartDate,
                                                  CultureInfo.InvariantCulture,
                                                  DateTimeStyles.RoundtripKind,
                                                  out DateTime startDate);
        bool isEndDateValid = DateTime.TryParse(dateRange.EndDate,
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.RoundtripKind,
                                                out DateTime endDate);

        if (isStartDateValid && isEndDateValid && startDate > endDate)
            AddFailure(context, $"{nameof(DateRange.StartDate)} must be earlier than {nameof(DateRange.EndDate)}");
    }

    /// <summary>Fails on duplicated keys and on keys outside <paramref name="validProperties"/>.</summary>
    protected void ValidateSortBy(IEnumerable<KeyValuePair<string, bool>>? sortProperty,
                                  IReadOnlySet<string> validProperties,
                                  ValidationContext<TRequest> context)
    {
        if (sortProperty is null)
            return;

        KeyValuePair<string, bool>[] sorts = [.. sortProperty];
        if (sorts.Length is 0)
            return;

        if (sorts.Select(pair => pair.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != sorts.Length)
            AddFailure(context, $"Property '{nameof(sortProperty)}' contains duplicated sorts.");

        foreach (KeyValuePair<string, bool> pair in sorts)
        {
            if (validProperties.Contains(pair.Key) is false)
                AddFailure(context, $"Property '{pair.Key}' is not a valid sort property.");
        }
    }

    /// <summary>True when the value is a syntactically valid email address.</summary>
    protected static bool IsValidEmail(string email) => MailAddress.TryCreate(email, out _);

    /// <summary>Adds one failure with the property name and the message, in the format the API returns.</summary>
    protected void AddFailure<T>(ValidationContext<T> context,
                                 string errorMessage,
                                 string? propertyName = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            propertyName = string.IsNullOrWhiteSpace(context.DisplayName)
                ? string.Empty
                : context.DisplayName.ToLowerInvariant();
        }

        var builder = new StringBuilder();

        if (string.IsNullOrWhiteSpace(propertyName) is false)
            builder.AppendLine(CultureInfo.InvariantCulture, $"Error while validating property: '{propertyName}' .");

        builder.Append(errorMessage);

        context.AddFailure(builder.ToString());
    }
}

/// <summary>
/// Strongly-typed input for start/end date validation. A record instead of an anonymous type: anonymous
/// types and <c>dynamic</c> are not Native AOT compatible.
/// </summary>
public sealed record DateRange(string? StartDate, string? EndDate);
