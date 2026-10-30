using CleanWebApiTemplate.Application.Abstractions;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Constants;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Todo;

/// <summary>
/// Shared input rules for the Todo validators. Uniqueness is deliberately not checked here:
/// it is a business rule owned by the application services, which can report it as a conflict.
/// </summary>
public class TodoValidator<TRequest> : BaseAbstractValidator<TRequest> where TRequest : class
{
    protected void ValidateTitleLength(string title, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
            return;
        }

        string trimmed = title.Trim();
        if (trimmed.Length > TodoTable.TitleMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoTable.TitleMaxLength}.");
    }

    protected void ValidateDescription(string description,
                                       ValidationContext<TRequest> context)
    {
        if (string.IsNullOrEmpty(description))
            return;

        if (description.Length > TodoTable.DescriptionMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoTable.DescriptionMaxLength}.");
    }

    protected void ValidateUserEmail(string createdBy,
                                     ValidationContext<TRequest> context)
    {
        if (IsValidEmail(createdBy) is false)
            AddFailure(context, $"Property '{context.DisplayName}' is not a valid email.");
    }

    protected void ValidateStatus(int status,
                                  ValidationContext<TRequest> context)
    {
        if (Enum.IsDefined(typeof(ETodoStatus), status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(ETodoStatus)}.");
    }
}
