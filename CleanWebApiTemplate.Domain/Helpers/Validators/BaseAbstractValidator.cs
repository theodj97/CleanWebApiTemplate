using FluentValidation;
using System.Net.Mail;

namespace CleanWebApiTemplate.Domain.Helpers.Validators;

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
            AddFailure(context, "Property '{0}' can't be null or empty!");
    }

    /// <summary>
    /// Common ULID validation logic.
    /// </summary>
    /// <param name="id">The ID value to validate</param>
    /// <param name="context">Validation context</param>
    protected void ValidateUlid(string id, ValidationContext<TCommand> context)
    {
        if (Ulid.TryParse(id, out _) is false)
            AddFailure(context, "Property '{0}' must be a valid ULID");

        if (id.Length is not 26)
            AddFailure(context, "Property '{0}' must have exactly 26 characters");
    }

    /// <summary>
    /// Validate a string that must be a valid DateTime.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="context"></param>
    protected void ValidateDateTime(string dateTime, ValidationContext<TCommand> context)
    {
        if (DateTime.TryParse(dateTime, out _) is false)
            AddFailure(context, "Property '{0}' must be a valid date time");
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
            context.AddFailure(nameof(startDateEndDate.StartDate),
                               $"{nameof(startDateEndDate.StartDate)} can't be null or empty when {nameof(startDateEndDate.EndDate)} has value");

        if (string.IsNullOrEmpty(startDateEndDate.EndDate)
        && string.IsNullOrEmpty(startDateEndDate.StartDate) is false)
            context.AddFailure(nameof(startDateEndDate.EndDate),
                               $"{nameof(startDateEndDate.EndDate)} can't be null or empty when {nameof(startDateEndDate.StartDate)} has value");

        if (DateTime.TryParse(startDateEndDate.StartDate, out var startDate) &&
                        DateTime.TryParse(startDateEndDate.EndDate, out var endDate))
            if (startDate > endDate)
                context.AddFailure(nameof(startDateEndDate.StartDate), $"{nameof(startDateEndDate.StartDate)} must be earlier than {nameof(startDateEndDate.EndDate)}");
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
    /// <param name="errorMessageTemplate"></param>
    /// <param name="args"></param>
    protected void AddFailure<T>(ValidationContext<T> context, string errorMessageTemplate, params object[] args)
    {
        string formattedMessage;
        string propertyName;
        if (string.IsNullOrEmpty(context.DisplayName))
            propertyName = string.Empty;
        else
            propertyName = context.DisplayName.ToLower();

        formattedMessage = string.Format(errorMessageTemplate, propertyName, args);
        context.AddFailure(propertyName, formattedMessage);
    }

    public class StartDateEndDateType(string? startDate, string? endDate)
    {
        public string StartDate = startDate ?? "";
        public string EndDate = endDate ?? "";
    }
}
