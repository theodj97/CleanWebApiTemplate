using FluentValidation;
using System.Net.Mail;
using System.Text;

namespace CleanWebApiTemplate.Application.Abstractions;

public class BaseAbstractValidator<TCommand> : AbstractValidator<TCommand> where TCommand : class
{
    public BaseAbstractValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
    }

    /// <summary>
    /// Validate if the property is not null or empty.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="context"></param>
    protected void NotNullNotEmpty(string property, ValidationContext<TCommand> context)
    {
        if (string.IsNullOrEmpty(property))
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
    }

    /// <summary>
    /// Common ULID validation logic.
    /// </summary>
    /// <param name="id">The ID value to validate</param>
    /// <param name="context">Validation context</param>
    protected void ValidateUlid(string id, ValidationContext<TCommand> context)
    {
        if (Ulid.TryParse(id, out _) is false)
            AddFailure(context, $"Property '{context.DisplayName}' must be a valid ULID");

        if (id.Length is not 26)
            AddFailure(context, $"Property '{context.DisplayName}' must have exactly 26 characters");
    }

    /// <summary>
    /// Validate a string that must be a valid DateTime.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="context"></param>
    protected void ValidateDateTime(string dateTime, ValidationContext<TCommand> context)
    {
        if (DateTime.TryParse(dateTime, out _) is false)
            AddFailure(context, $"Property '{context.DisplayName}' must be a valid date time");
    }

    /// <summary>
    /// Validate a StartDate and EndDate in a request.
    /// </summary>
    /// <param name="dateRange"></param>
    /// <param name="context"></param>
    protected void ValidateStartDateAndEndDate(DateRange dateRange, ValidationContext<TCommand> context)
    {
        if (string.IsNullOrEmpty(dateRange.StartDate)
            && string.IsNullOrEmpty(dateRange.EndDate) is false)
            AddFailure(context, $"{nameof(DateRange.StartDate)} can't be null or empty when {nameof(DateRange.EndDate)} has value");

        if (string.IsNullOrEmpty(dateRange.EndDate)
        && string.IsNullOrEmpty(dateRange.StartDate) is false)
            AddFailure(context, $"{nameof(DateRange.EndDate)} can't be null or empty when {nameof(DateRange.StartDate)} has value");

        bool isStartDateValid = DateTime.TryParse(dateRange.StartDate, out DateTime startDate);
        bool isEndDateValid = DateTime.TryParse(dateRange.EndDate, out DateTime endDate);

        if (isStartDateValid && isEndDateValid)
            if (startDate > endDate)
                AddFailure(context, $"{nameof(DateRange.StartDate)} must be earlier than {nameof(DateRange.EndDate)}");

    }


    /// <summary>
    /// Validate the sorting properties.
    /// </summary>
    /// <param name="sortProperty"></param>
    /// <param name="validProperties"></param>
    /// <param name="context"></param>
    protected void ValidateSortBy(IEnumerable<KeyValuePair<string, bool>>? sortProperty,
                                  HashSet<string> validProperties,
                                  ValidationContext<TCommand> context)
    {
        if (sortProperty is null || sortProperty.Any() is false) return;

        if (sortProperty!.Select(kvp => kvp.Key).Distinct().Count() != sortProperty!.Count())
            AddFailure(context, $"Property '{nameof(sortProperty)}' contains duplicated sorts.");

        foreach (var property in sortProperty!)
            if (!validProperties.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                AddFailure(context, $"Property '{property.Key}' is not a valid sort property.");
    }

    /// <summary>
    /// Validate a email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns>True if email is valid, False if unvalid.</returns>
    protected bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extension to add a failure to the context.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="errorMessage"></param>
    protected void AddFailure<T>(ValidationContext<T> context,
                                 string errorMessage,
                                 string? propertyName = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            propertyName = !string.IsNullOrWhiteSpace(context.DisplayName)
                ? context.DisplayName.ToLower()
                : string.Empty;
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(propertyName))
            builder.AppendLine($"Error while validanting property: '{propertyName}' .");

        builder.Append(errorMessage);

        context.AddFailure(builder.ToString());
    }
}

/// <summary>
/// Strongly-typed input for StartDate/EndDate cross-property validation.
/// Replaces the previous anonymous type + dynamic parameter (not Native AOT compatible).
/// </summary>
public sealed record DateRange(string? StartDate, string? EndDate);
