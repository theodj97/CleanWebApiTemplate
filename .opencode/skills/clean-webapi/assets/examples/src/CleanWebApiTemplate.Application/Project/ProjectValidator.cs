using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Project;

/// <summary>
/// Shared input rules for the Project validators. Uniqueness is deliberately not checked here:
/// it is a business rule owned by the application handlers, which can report it as a conflict.
/// </summary>
public abstract class ProjectValidator<TRequest> : BaseAbstractValidator<TRequest> where TRequest : class
{
    protected void ValidateNameLength(string name, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
            return;
        }

        if (name.Trim().Length > ProjectConstraints.NameMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {ProjectConstraints.NameMaxLength}.");
    }

    protected void ValidateDescription(string description, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrEmpty(description))
            return;

        if (description.Length > ProjectConstraints.DescriptionMaxLength)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {ProjectConstraints.DescriptionMaxLength}.");
    }

    protected void ValidateOwnerEmail(string owner, ValidationContext<TRequest> context)
    {
        if (IsValidEmail(owner) is false)
            AddFailure(context, $"Property '{context.DisplayName}' is not a valid email.");
    }

    protected void ValidateStatus(int status, ValidationContext<TRequest> context)
    {
        if (Enum.IsDefined((EProjectStatus)status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(EProjectStatus)}.");
    }
}
