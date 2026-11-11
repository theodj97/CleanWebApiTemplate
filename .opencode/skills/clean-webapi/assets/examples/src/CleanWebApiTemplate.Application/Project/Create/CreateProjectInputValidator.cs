using FluentValidation;

namespace CleanWebApiTemplate.Application.Project.Create;

public sealed class CreateProjectInputValidator : ProjectValidator<CreateProjectInput>
{
    public CreateProjectInputValidator()
    {
        RuleFor(x => x.Name)
            .Custom(NotNullNotEmpty)
            .Custom((name, context) => ValidateNameLength(name!, context));

        RuleFor(x => x.Owner)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateOwnerEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription)
            .When(x => string.IsNullOrEmpty(x.Description) is false);
    }
}
