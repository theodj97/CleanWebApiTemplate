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

    protected void ValidateEmail(string email, ValidationContext<TCommand> context)
    {

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
        var propertyName = context.DisplayName.ToLower();
        var formattedMessage = string.Format(errorMessageTemplate, propertyName, args);
        context.AddFailure(propertyName, formattedMessage);
    }
}
