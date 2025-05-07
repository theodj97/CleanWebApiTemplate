using FluentValidation;
using System.Net.Mail;
using System.Reflection;
using System.Text;

namespace CleanWebApiTemplate.Application.Helpers.Validators;

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
    /// <param name="propertyName"></param>
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
    /// <param name="startDateEndDate"></param>
    /// <param name="context"></param>
    protected void ValidateStartDateAndEndDate(StartDateEndDateType startDateEndDate, ValidationContext<TCommand> context)
    {
        if (string.IsNullOrEmpty(startDateEndDate.StartDate)
            && string.IsNullOrEmpty(startDateEndDate.EndDate) is false)
            AddFailure(context, $"{nameof(startDateEndDate.StartDate)} can't be null or empty when {nameof(startDateEndDate.EndDate)} has value");

        if (string.IsNullOrEmpty(startDateEndDate.EndDate)
        && string.IsNullOrEmpty(startDateEndDate.StartDate) is false)
            AddFailure(context, $"{nameof(startDateEndDate.EndDate)} can't be null or empty when {nameof(startDateEndDate.StartDate)} has value");

        if (DateTime.TryParse(startDateEndDate.StartDate, out var startDate) &&
                        DateTime.TryParse(startDateEndDate.EndDate, out var endDate))
            if (startDate > endDate)
                AddFailure(context, $"{nameof(startDateEndDate.StartDate)} must be earlier than {nameof(startDateEndDate.EndDate)}");
    }

    protected void ValidateSortBy(IEnumerable<KeyValuePair<string, bool>>? sortProperty,
                                  Type typeToSortBy,
                                  ValidationContext<TCommand> context)
    {
        if (sortProperty is null || sortProperty.Any() is false) return;

        if (sortProperty!.Select(kvp => kvp.Key).Distinct().Count() != sortProperty!.Count())
            AddFailure(context, $"Property '{nameof(sortProperty)}' contains duplicated sorts.");

        var typeToSortByProperties = typeToSortBy.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Select(p => p.Name)
                                                 .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in sortProperty!)
            if (!typeToSortByProperties.Contains(property.Key))
                AddFailure(context, $"Property '{property.Key}' is not a valid property of type {typeToSortBy.Name}.");
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
    /// <param name="args"></param>
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

    public class StartDateEndDateType(string? startDate, string? endDate)
    {
        public string StartDate = startDate ?? "";
        public string EndDate = endDate ?? "";
    }
}
