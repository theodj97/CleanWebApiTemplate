using {App}.Application.Validation;
using {App}.Domain.Models.Enums.Project;
using {App}.Domain.Models.Project;
using FluentValidation;

namespace {App}.Application.Project.Handlers;

/// <summary>
/// Shared rules for the Project validators. Uniqueness is deliberately not checked here: it is a
/// business rule owned by the application handlers, which can report it as a conflict.
/// </summary>
public class ProjectValidator<TRequest> : BaseAbstractValidator<TRequest> where TRequest : class
{
    protected void ValidateName(string name, ValidationContext<TRequest> context)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AddFailure(context, $"Property '{context.DisplayName}' can't be null or empty!");
            return;
        }

        string trimmed = name.Trim();
        if (trimmed.Length > ProjectConstraints.NameMaxLength)
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
        if (Enum.IsDefined(typeof(EProjectStatus), status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(EProjectStatus)}.");
    }
}
