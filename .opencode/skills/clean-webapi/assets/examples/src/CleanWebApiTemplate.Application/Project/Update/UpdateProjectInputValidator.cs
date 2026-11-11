using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.Update;

public sealed class UpdateProjectInputValidator : ProjectValidator<UpdateProjectInput>
{
    public UpdateProjectInputValidator()
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);

        RuleFor(x => x.Name)
            .Custom((name, context) => ValidateNameLength(name!, context))
            .When(x => x.Name is not null);

        RuleFor(x => x.LastWriter)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription!)
            .When(x => string.IsNullOrEmpty(x.Description) is false);

        RuleFor(x => x.Status!.Value)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null);
    }
}
