using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Todo;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo;

/// <summary>
/// Shared input rules for the Todo validators. Uniqueness is deliberately not checked here:
/// it is a business rule owned by the application handlers, which can report it as a conflict.
/// </summary>
public abstract class TodoValidator<TRequest> : BaseAbstractValidator<TRequest> where TRequest : class
{
    protected void ValidateTitleLength(string title, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
            return;
        }

        if (title.Trim().Length > TodoConstraints.TitleMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoConstraints.TitleMaxLength}.");
    }

    protected void ValidateDescription(string description, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrEmpty(description))
            return;

        if (description.Length > TodoConstraints.DescriptionMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoConstraints.DescriptionMaxLength}.");
    }

    protected void ValidateOwnerEmail(string owner, ValidationContext<TRequest> context)
    {
        if (IsValidEmail(owner) is false)
            AddFailure(context, $"Property '{context.DisplayName}' is not a valid email.");
    }

    protected void ValidateStatus(int status, ValidationContext<TRequest> context)
    {
        if (Enum.IsDefined((ETodoStatus)status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(ETodoStatus)}.");
    }
}
